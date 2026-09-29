(function () {
  const AZUL = [29, 91, 136];
  const CLARO = [214, 228, 240];
  const TINTA = [34, 34, 34];
  const GRIS = [110, 110, 110];
  const LINEA = [170, 170, 170];

  const txt = function (v) { return v === null || v === undefined ? "" : String(v); };
  const dinero = function (n) {
    return "$ " + Number(n || 0).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  };
  const cant = function (n) {
    n = Number(n || 0);
    return Number.isInteger(n) ? String(n) : n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  };
  const medida = function (n) { return (Math.round(Number(n || 0) * 100 + 1e-7) / 100).toFixed(2); };
  const pct = function (n) { return (Math.round(Number(n || 0) * 10000) / 100) + "%"; };

  function imagen(doc, im, x, y, w, h) {
    if (!im) return;
    try { doc.addImage(im.data, im.tipo, x, y, w, h); } catch (e) { }
  }

  function ajustar(im, wMax, hMax) {
    if (!im) return { w: 0, h: 0 };
    let w = wMax, h = w * im.h / im.w;
    if (h > hMax) { h = hMax; w = h * im.w / im.h; }
    return { w: w, h: h };
  }

  async function construir(d) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: "pt", format: "letter" });
    const IM = await window.cotizadorImagenes.cargar();
    const W = doc.internal.pageSize.getWidth();
    const H = doc.internal.pageSize.getHeight();
    const M = 36;
    const pieH = W * 200 / 2550;
    const LIMITE = H - pieH - 18;
    const t = d.totales;
    let y = 26;

    const nuevaPagina = function () { doc.addPage(); y = 36; };
    const espacio = function (h) { if (y + h > LIMITE) nuevaPagina(); };

    let s = ajustar(IM.sanimax, 78, 62);
    imagen(doc, IM.sanimax, M, y, s.w, s.h);
    s = ajustar(IM.modumex, 160, 54);
    imagen(doc, IM.modumex, (W - s.w) / 2, y + 4, s.w, s.h);
    s = ajustar(IM.steelcore, 112, 50);
    imagen(doc, IM.steelcore, W - M - s.w, y + 6, s.w, s.h);
    y += 72;

    doc.setFillColor(...CLARO); doc.setDrawColor(...LINEA);
    doc.rect(M, y, W - 2 * M - 150, 18, "FD");
    doc.setFont("helvetica", "bold"); doc.setFontSize(10); doc.setTextColor(...TINTA);
    doc.text("PRESUPUESTO " + txt(d.folio), M + (W - 2 * M - 150) / 2, y + 12.5, { align: "center" });
    doc.setFontSize(9);
    doc.text(txt(d.fecha).toUpperCase(), W - M, y + 12.5, { align: "right" });
    y += 28;

    const yInfo = y;
    doc.setFontSize(8.5);
    const campo = function (etq, val, yy) {
      doc.setFont("helvetica", "bold"); doc.setTextColor(...TINTA);
      doc.text(etq, M + 58, yy, { align: "right" });
      doc.setFont("helvetica", "normal");
      const v = doc.splitTextToSize(txt(val), 250);
      doc.text(v[0] || "", M + 66, yy);
      doc.setDrawColor(...LINEA); doc.line(M + 64, yy + 3, M + 320, yy + 3);
    };
    campo("Obra:", d.obra, y + 12);
    campo("Contacto:", d.contacto, y + 28);
    campo("Ubicación:", d.ubicacion, y + 44);

    const esp = [
      ["Marca", d.marca], ["Distribuidor", d.distribuidor], ["Modelo", d.linea], ["Acabado", d.acabado],
      ["Color", d.color], ["Color de marco", d.colorMarco], ["Perfil", d.colorPerfil]
    ];
    doc.autoTable({
      startY: yInfo,
      margin: { left: W - M - 210, right: M },
      tableWidth: 210,
      theme: "grid",
      head: [[{ content: "ESPECIFICACIONES", colSpan: 2, styles: { halign: "center" } }]],
      body: esp.map(function (r) { return [r[0] + ":", txt(r[1]).toUpperCase()]; }),
      styles: { fontSize: 7.5, cellPadding: 2.2, lineColor: LINEA, lineWidth: 0.4, textColor: TINTA },
      headStyles: { fillColor: CLARO, textColor: TINTA, fontStyle: "bold" },
      columnStyles: { 0: { halign: "right", cellWidth: 72, fontStyle: "bold" }, 1: { halign: "center" } }
    });
    y = Math.max(doc.lastAutoTable.finalY, yInfo + 52) + 12;

    if (txt(d.especificacion).trim()) {
      doc.setFontSize(8);
      const lineas = doc.splitTextToSize(txt(d.especificacion), W - 2 * M - 16);
      const alto = 20 + lineas.length * 10;
      espacio(alto);
      doc.setDrawColor(...AZUL); doc.setLineWidth(0.8);
      doc.rect(M, y, W - 2 * M, alto);
      doc.setLineWidth(0.4);
      doc.setFont("helvetica", "bold"); doc.setTextColor(...AZUL);
      doc.text("ESPECIFICACIÓN", M + 8, y + 12);
      doc.setFont("helvetica", "normal"); doc.setTextColor(...TINTA);
      doc.text(lineas, M + 8, y + 24);
      y += alto + 12;
    }

    const base = {
      theme: "grid",
      margin: { left: M, right: M, bottom: H - LIMITE },
      styles: { fontSize: 7.5, cellPadding: 2.4, lineColor: LINEA, lineWidth: 0.4, textColor: TINTA },
      headStyles: { fillColor: CLARO, textColor: TINTA, fontStyle: "bold", halign: "center" }
    };

    (d.partidas || []).forEach(function (p) {
      espacio(120);
      const titulo = txt(p.clave) + (p.ubicacion ? "   " + txt(p.ubicacion).toUpperCase() : "");
      doc.autoTable(Object.assign({}, base, {
        startY: y,
        head: [
          [{ content: titulo, colSpan: 3, styles: { halign: "left", fillColor: AZUL, textColor: [255, 255, 255] } },
           { content: "TOTAL DE PUERTAS: " + p.cantidad, colSpan: 3, styles: { halign: "right", fillColor: AZUL, textColor: [255, 255, 255] } }],
          ["Cantidad", "Producto", "Medida de puerta por hoja", "Vano", "Precio Unit.", "Precio Total"]
        ],
        body: [[cant(p.cantidad), p.producto, medida(p.hojaAncho) + " x " + medida(p.hojaAlto),
                medida(p.vanoAncho) + " x " + medida(p.vanoAlto), dinero(p.precioUnitario), dinero(p.total)]],
        columnStyles: { 0: { halign: "center", cellWidth: 50 }, 2: { halign: "center" }, 3: { halign: "center" },
                        4: { halign: "right", cellWidth: 78 }, 5: { halign: "right", cellWidth: 84, fontStyle: "bold" } }
      }));
      y = doc.lastAutoTable.finalY;
      doc.autoTable(Object.assign({}, base, {
        startY: y,
        head: [
          [{ content: "EXPLOSIÓN DE INSUMOS (POR PUERTA)", colSpan: 4, styles: { halign: "left" } }],
          ["Cantidad", "Producto", "Precio Unitario", "Precio total"]
        ],
        body: (p.insumos || []).map(function (i) { return [cant(i.cantidad), txt(i.descripcion), dinero(i.precio), dinero(i.total)]; }),
        foot: [[{ content: "PRECIO UNITARIO", colSpan: 3, styles: { halign: "right" } }, dinero(p.precioUnitario)]],
        footStyles: { fillColor: [245, 245, 245], textColor: TINTA, fontStyle: "bold", halign: "right" },
        styles: Object.assign({}, base.styles, { fontSize: 7 }),
        columnStyles: { 0: { halign: "center", cellWidth: 50 }, 2: { halign: "right", cellWidth: 78 }, 3: { halign: "right", cellWidth: 84 } }
      }));
      y = doc.lastAutoTable.finalY + 12;
    });

    const filas = [];
    filas.push(["Material:", dinero(t.material)]);
    (t.descuentos || []).forEach(function (x) { filas.push([txt(x.nombre) + " " + pct(x.porcentaje / 100) + ":", "- " + dinero(x.monto)]); });
    if (t.instalacion) filas.push(["Instalación:", dinero(t.instalacion)]);
    if (t.flete) filas.push(["Flete:", dinero(t.flete)]);
    if (t.viaticos) filas.push(["Viáticos:", dinero(t.viaticos)]);
    (t.extras || []).forEach(function (x) { filas.push([txt(x.nombre) + ":", dinero(x.monto)]); });
    filas.push(["Subtotal:", dinero(t.subtotal)]);
    filas.push(["IVA " + pct(t.ivaPct) + ":", dinero(t.iva)]);
    filas.push(["Gran Total:", dinero(t.total)]);

    espacio(40 + filas.length * 13);
    const yRes = y;
    doc.autoTable(Object.assign({}, base, {
      startY: y,
      margin: { left: W - M - 250, right: M, bottom: H - LIMITE },
      tableWidth: 250,
      head: [[{ content: t.puertas + " PUERTAS TOTALES", colSpan: 2 }]],
      body: filas,
      styles: Object.assign({}, base.styles, { fontSize: 8 }),
      columnStyles: { 0: { halign: "right", fontStyle: "bold" }, 1: { halign: "right", cellWidth: 100 } },
      didParseCell: function (h) {
        if (h.section === "body" && h.row.index === filas.length - 1) { h.cell.styles.fillColor = CLARO; h.cell.styles.fontSize = 9; h.cell.styles.fontStyle = "bold"; }
      }
    }));
    const yFinRes = doc.lastAutoTable.finalY;
    doc.setFont("helvetica", "bold"); doc.setFontSize(7.5); doc.setTextColor(...GRIS);
    doc.text("PRECIOS EN " + txt(d.moneda) + (d.moneda === "USD" ? " (DÓLARES)" : " (PESOS MEXICANOS)"), W - M, yFinRes + 10, { align: "right" });

    doc.autoTable(Object.assign({}, base, {
      startY: yRes,
      margin: { left: M, right: W - M - 262, bottom: H - LIMITE },
      tableWidth: 262,
      head: [[{ content: "CONDICIONES DE PAGO", colSpan: 3 }]],
      body: [
        ["ANTICIPO " + pct(t.anticipoPct), "PARA INICIAR FABRICACIÓN", dinero(t.anticipo)],
        ["SALDO " + pct(1 - t.anticipoPct), "SOBRE AVISO DE EMBARQUE", dinero(t.saldo)],
        [{ content: "TOTAL", colSpan: 2, styles: { halign: "right", fontStyle: "bold" } }, { content: dinero(t.total), styles: { fontStyle: "bold" } }]
      ],
      styles: Object.assign({}, base.styles, { fontSize: 7.5 }),
      columnStyles: { 0: { fontStyle: "bold", cellWidth: 60 }, 2: { halign: "right", cellWidth: 78 } }
    }));
    let yIzq = doc.lastAutoTable.finalY + 12;

    const banco = txt(d.banco).split(/\r?\n/).filter(function (l) { return l.trim(); });
    if (banco.length) {
      const alto = 18 + banco.length * 10;
      doc.setDrawColor(...LINEA); doc.rect(M, yIzq, 262, alto);
      doc.setFont("helvetica", "bold"); doc.setFontSize(8); doc.setTextColor(...AZUL);
      doc.text("DATOS BANCARIOS" + (d.moneda === "USD" ? " (DÓLARES)" : " (PESOS)"), M + 6, yIzq + 11);
      doc.setFont("helvetica", "normal"); doc.setTextColor(...TINTA); doc.setFontSize(7.5);
      doc.text(banco, M + 6, yIzq + 22);
      yIzq += alto;
    }
    y = Math.max(yIzq, yFinRes + 14) + 14;

    const bloque = function (titulo, texto) {
      const lineas = txt(texto).split(/\r?\n/).filter(function (l) { return l.trim(); });
      if (!lineas.length) return;
      doc.setFontSize(7.5);
      const partes = [];
      lineas.forEach(function (l) { doc.splitTextToSize(l, W - 2 * M).forEach(function (x) { partes.push(x); }); });
      espacio(16 + partes.length * 9.5);
      doc.setFont("helvetica", "bold"); doc.setFontSize(8.5); doc.setTextColor(...AZUL);
      doc.text(titulo, M, y);
      doc.setDrawColor(...AZUL); doc.line(M, y + 2, M + doc.getTextWidth(titulo), y + 2);
      doc.setFont("helvetica", "normal"); doc.setFontSize(7.5); doc.setTextColor(...TINTA);
      doc.text(partes, M, y + 12);
      y += 14 + partes.length * 9.5 + 6;
    };
    bloque("TIEMPO DE ENTREGA:", d.tiempoEntrega);
    bloque("OBSERVACIONES:", d.observaciones);

    espacio(70);
    y += 36;
    doc.setDrawColor(...TINTA); doc.setLineWidth(0.6);
    doc.line(M + 10, y, M + 190, y);
    doc.line(W - M - 190, y, W - M - 10, y);
    doc.setLineWidth(0.4);
    doc.setFont("helvetica", "bold"); doc.setFontSize(7.5); doc.setTextColor(...TINTA);
    const firma = txt(d.firma).split(/\r?\n/).filter(function (l) { return l.trim(); });
    firma.forEach(function (l, i) { doc.text(l, M + 100, y + 10 + i * 9, { align: "center" }); });
    doc.text(txt(d.contacto).toUpperCase() || "CLIENTE", W - M - 100, y + 10, { align: "center" });
    doc.text("ACEPTO PRESUPUESTO", W - M - 100, y + 19, { align: "center" });

    doc.addPage(); y = 40;
    doc.setFont("helvetica", "bold"); doc.setFontSize(11); doc.setTextColor(...AZUL);
    doc.text("CARACTERÍSTICAS DE LA PUERTA MODELO STEELCORE", M, y);
    y += 10;
    s = ajustar(IM.corte, W - 2 * M, 330);
    imagen(doc, IM.corte, (W - s.w) / 2, y, s.w, s.h);
    y += s.h + 24;
    doc.text("CARACTERÍSTICAS DEL ACABADO SÓLIDO FENÓLICO", M, y);
    y += 10;
    s = ajustar(IM.acabado, W - 2 * M, LIMITE - y);
    imagen(doc, IM.acabado, (W - s.w) / 2, y, s.w, s.h);

    const n = doc.internal.getNumberOfPages();
    for (let i = 1; i <= n; i++) {
      doc.setPage(i);
      imagen(doc, IM.pie, 0, H - pieH, W, pieH);
      doc.setFont("helvetica", "normal"); doc.setFontSize(6.5); doc.setTextColor(...GRIS);
      doc.text(txt(d.folio) + "  ·  Página " + i + " de " + n, W - M, H - pieH - 5, { align: "right" });
    }
    return doc;
  }

  window.cotizadorPdf = {
    construir: construir,
    descargar: async function (d) {
      const doc = await construir(d);
      doc.save((d.archivo || "Cotizacion") + ".pdf");
    }
  };
})();
