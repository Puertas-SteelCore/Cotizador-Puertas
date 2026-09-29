(function () {
  const AZUL = "FF1D5B88";
  const CLARO = "FFD6E4F0";
  const GRIS = "FFAAAAAA";
  const BLANCO = "FFFFFFFF";
  const bd = { style: "thin", color: { argb: GRIS } };
  const BORDE = { top: bd, bottom: bd, left: bd, right: bd };
  const fondo = function (argb) { return { type: "pattern", pattern: "solid", fgColor: { argb: argb } }; };
  const MONEDA = '"$"#,##0.00';
  const txt = function (v) { return v === null || v === undefined ? "" : String(v); };
  const med = function (n) { return (Math.round(Number(n || 0) * 100 + 1e-7) / 100).toFixed(2); };

  function descargarBuffer(buf, nombre) {
    const blob = new Blob([buf], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = nombre;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 1500);
  }

  async function construir(d) {
    const IM = await window.cotizadorImagenes.cargar();
    const wb = new window.ExcelJS.Workbook();
    wb.creator = "Puertas Modumex";
    const ws = wb.addWorksheet("Cotización", {
      views: [{ showGridLines: false }],
      pageSetup: { paperSize: 1, orientation: "portrait", fitToPage: true, fitToWidth: 1, fitToHeight: 0,
                   margins: { left: 0.4, right: 0.4, top: 0.4, bottom: 0.5, header: 0.2, footer: 0.2 } }
    });
    ws.columns = [{ width: 11 }, { width: 44 }, { width: 18 }, { width: 14 }, { width: 16 }, { width: 18 }];
    const t = d.totales;

    const logo = function (im, col, row, wMax, hMax) {
      if (!im) return;
      let w = wMax, h = w * im.h / im.w;
      if (h > hMax) { h = hMax; w = h * im.w / im.h; }
      const id = wb.addImage({ base64: im.data, extension: im.tipo === "PNG" ? "png" : "jpeg" });
      ws.addImage(id, { tl: { col: col, row: row }, ext: { width: w, height: h } });
    };
    for (let r = 1; r <= 5; r++) ws.getRow(r).height = 16;
    logo(IM.sanimax, 0.1, 0.2, 95, 76);
    logo(IM.modumex, 1.55, 0.6, 200, 66);
    logo(IM.steelcore, 4.3, 0.8, 140, 60);

    let r = 7;
    const celda = function (ref, valor, estilo) {
      const c = ws.getCell(ref);
      c.value = valor;
      if (estilo) Object.assign(c, estilo);
      return c;
    };
    const unir = function (a, b) { ws.mergeCells(a + ":" + b); };
    const cab = function (c, relleno, color) {
      c.fill = fondo(relleno || CLARO);
      c.font = { bold: true, size: 9, color: { argb: color || "FF222222" } };
      c.alignment = { horizontal: "center", vertical: "middle", wrapText: true };
      c.border = BORDE;
    };
    const normal = function (c, alin, num) {
      c.font = { size: 9 };
      c.alignment = { horizontal: alin || "left", vertical: "middle", wrapText: true };
      c.border = BORDE;
      if (num) c.numFmt = num;
    };

    unir("A" + r, "D" + r);
    cab(celda("A" + r, "PRESUPUESTO " + txt(d.folio)));
    ws.getCell("A" + r).font = { bold: true, size: 11 };
    unir("E" + r, "F" + r);
    celda("E" + r, txt(d.fecha).toUpperCase(), { font: { bold: true, size: 9 }, alignment: { horizontal: "right" } });
    r += 2;

    const info = [["Obra:", d.obra], ["Contacto:", d.contacto], ["Ubicación:", d.ubicacion]];
    const esp = [["Marca:", d.marca], ["Distribuidor:", d.distribuidor], ["Modelo:", d.linea], ["Acabado:", d.acabado],
                 ["Color:", d.color], ["Color de marco:", d.colorMarco], ["Perfil:", d.colorPerfil]];
    unir("E" + r, "F" + r);
    cab(celda("E" + r, "ESPECIFICACIONES"));
    const r0 = r + 1;
    esp.forEach(function (e, i) {
      const c1 = celda("E" + (r0 + i), e[0]); normal(c1, "right"); c1.font = { bold: true, size: 9 };
      const c2 = celda("F" + (r0 + i), txt(e[1]).toUpperCase()); normal(c2, "center");
    });
    info.forEach(function (e, i) {
      const rr = r0 + i * 2;
      celda("A" + rr, e[0], { font: { bold: true, size: 9 }, alignment: { horizontal: "right" } });
      unir("B" + rr, "C" + rr);
      celda("B" + rr, txt(e[1]), { font: { size: 9 }, border: { bottom: bd } });
    });
    r = r0 + esp.length + 1;

    if (txt(d.especificacion).trim()) {
      celda("A" + r, "ESPECIFICACIÓN", { font: { bold: true, size: 9, color: { argb: AZUL } } });
      r++;
      unir("A" + r, "F" + r);
      const c = celda("A" + r, txt(d.especificacion));
      c.font = { size: 9 };
      c.alignment = { wrapText: true, vertical: "top" };
      c.border = { top: { style: "thin", color: { argb: AZUL } }, bottom: { style: "thin", color: { argb: AZUL } },
                   left: { style: "thin", color: { argb: AZUL } }, right: { style: "thin", color: { argb: AZUL } } };
      ws.getRow(r).height = Math.max(30, Math.ceil(txt(d.especificacion).length / 115) * 13);
      r += 2;
    }

    const totalesPartidas = [];
    (d.partidas || []).forEach(function (p) {
      unir("A" + r, "D" + r);
      cab(celda("A" + r, txt(p.clave) + (p.ubicacion ? "   " + txt(p.ubicacion).toUpperCase() : "")), AZUL, BLANCO);
      ws.getCell("A" + r).alignment = { horizontal: "left", vertical: "middle" };
      unir("E" + r, "F" + r);
      cab(celda("E" + r, "TOTAL DE PUERTAS: " + p.cantidad), AZUL, BLANCO);
      r++;
      ["Cantidad", "Producto", "Medida de puerta por hoja", "Vano", "Precio Unit.", "Precio Total"].forEach(function (h, i) {
        cab(ws.getCell(r, i + 1)); ws.getCell(r, i + 1).value = h;
      });
      ws.getRow(r).height = 24;
      r++;
      const filaPuerta = r;
      normal(celda("A" + r, p.cantidad), "center");
      normal(celda("B" + r, p.producto));
      normal(celda("C" + r, med(p.hojaAncho) + " x " + med(p.hojaAlto)), "center");
      normal(celda("D" + r, med(p.vanoAncho) + " x " + med(p.vanoAlto)), "center");
      r += 2;

      unir("A" + r, "F" + r);
      cab(celda("A" + r, "EXPLOSIÓN DE INSUMOS (POR PUERTA)"));
      ws.getCell("A" + r).alignment = { horizontal: "left" };
      r++;
      cab(celda("A" + r, "Cantidad"));
      unir("B" + r, "D" + r); cab(celda("B" + r, "Producto"));
      cab(celda("E" + r, "Precio Unitario"));
      cab(celda("F" + r, "Precio total"));
      r++;
      const ini = r;
      (p.insumos || []).forEach(function (i) {
        normal(celda("A" + r, i.cantidad), "center", "General");
        unir("B" + r, "D" + r);
        normal(celda("B" + r, txt(i.descripcion)));
        normal(celda("E" + r, i.precio), "right", MONEDA);
        normal(celda("F" + r, { formula: "A" + r + "*E" + r, result: i.total }), "right", MONEDA);
        r++;
      });
      unir("A" + r, "E" + r);
      const ct = celda("A" + r, "PRECIO UNITARIO");
      ct.font = { bold: true, size: 9 }; ct.alignment = { horizontal: "right" };
      const pu = celda("F" + r, { formula: (p.insumos || []).length ? "SUM(F" + ini + ":F" + (r - 1) + ")" : "0", result: p.precioUnitario });
      normal(pu, "right", MONEDA); pu.font = { bold: true, size: 9 };
      normal(celda("E" + filaPuerta, { formula: "F" + r, result: p.precioUnitario }), "right", MONEDA);
      const tot = celda("F" + filaPuerta, { formula: "A" + filaPuerta + "*E" + filaPuerta, result: p.total });
      normal(tot, "right", MONEDA); tot.font = { bold: true, size: 9 };
      totalesPartidas.push("F" + filaPuerta);
      r += 2;
    });

    const fila = function (etq, valor, negrita) {
      unir("C" + r, "E" + r);
      const a = celda("C" + r, etq);
      a.font = { bold: true, size: 9 }; a.alignment = { horizontal: "right" };
      const b = celda("F" + r, valor);
      normal(b, "right", MONEDA);
      if (negrita) { b.font = { bold: true, size: 10 }; b.fill = fondo(CLARO); }
      return r++;
    };
    celda("A" + r, t.puertas, { font: { bold: true, size: 9 }, alignment: { horizontal: "center" }, border: BORDE });
    celda("B" + r, "PUERTAS TOTALES", { font: { bold: true, size: 9 } });
    const fMat = fila("Material:", { formula: totalesPartidas.length ? totalesPartidas.join("+") : "0", result: t.material });
    const suman = ["F" + fMat];
    (t.descuentos || []).forEach(function (x) {
      const f = fila(txt(x.nombre) + " " + x.porcentaje + "%:", { formula: "-F" + fMat + "*" + (x.porcentaje / 100), result: -x.monto });
      suman.push("F" + f);
    });
    if (t.instalacion) suman.push("F" + fila("Instalación:", t.instalacion));
    if (t.flete) suman.push("F" + fila("Flete:", t.flete));
    if (t.viaticos) suman.push("F" + fila("Viáticos:", t.viaticos));
    (t.extras || []).forEach(function (x) { suman.push("F" + fila(txt(x.nombre) + ":", x.monto)); });
    const fSub = fila("Subtotal:", { formula: suman.join("+"), result: t.subtotal });
    const fIva = fila("IVA " + Math.round(t.ivaPct * 100) + "%:", { formula: "F" + fSub + "*" + t.ivaPct, result: t.iva });
    const fTot = fila("Gran Total:", { formula: "F" + fSub + "+F" + fIva, result: t.total }, true);
    celda("F" + r, "PRECIOS EN " + txt(d.moneda), { font: { bold: true, size: 8 }, alignment: { horizontal: "right" } });
    r += 2;

    unir("A" + r, "C" + r);
    cab(celda("A" + r, "CONDICIONES DE PAGO"));
    r++;
    const ant = Math.round(t.anticipoPct * 100);
    const rAnt = r;
    [["ANTICIPO " + ant + "%", "PARA INICIAR FABRICACIÓN", { formula: "F" + fTot + "*" + t.anticipoPct, result: t.anticipo }],
     ["SALDO " + (100 - ant) + "%", "SOBRE AVISO DE EMBARQUE", { formula: "F" + fTot + "-C" + rAnt, result: t.saldo }],
     ["TOTAL", "", { formula: "F" + fTot, result: t.total }]].forEach(function (x) {
      const a = celda("A" + r, x[0]); normal(a); a.font = { bold: true, size: 9 };
      normal(celda("B" + r, x[1]));
      normal(celda("C" + r, x[2]), "right", MONEDA);
      r++;
    });
    r++;

    const bloque = function (titulo, texto) {
      const lineas = txt(texto).split(/\r?\n/).filter(function (l) { return l.trim(); });
      if (!lineas.length) return;
      celda("A" + r, titulo, { font: { bold: true, size: 9, color: { argb: AZUL }, underline: true } });
      r++;
      lineas.forEach(function (l) {
        unir("A" + r, "F" + r);
        celda("A" + r, l, { font: { size: 9 }, alignment: { wrapText: true } });
        if (l.length > 110) ws.getRow(r).height = 24;
        r++;
      });
      r++;
    };
    bloque("DATOS BANCARIOS" + (d.moneda === "USD" ? " (DÓLARES):" : " (PESOS):"), d.banco);
    bloque("TIEMPO DE ENTREGA:", d.tiempoEntrega);
    bloque("OBSERVACIONES:", d.observaciones);

    r += 2;
    const firma = txt(d.firma).split(/\r?\n/).filter(function (l) { return l.trim(); });
    ws.getCell("B" + r).border = { top: { style: "thin" } };
    unir("E" + r, "F" + r);
    ws.getCell("E" + r).border = { top: { style: "thin" } };
    firma.forEach(function (l, i) { celda("B" + (r + i), l, { font: { bold: true, size: 8 }, alignment: { horizontal: "center" } }); });
    celda("E" + r, txt(d.contacto).toUpperCase() || "CLIENTE", { font: { bold: true, size: 8 }, alignment: { horizontal: "center" }, border: { top: { style: "thin" } } });
    unir("E" + (r + 1), "F" + (r + 1));
    celda("E" + (r + 1), "ACEPTO PRESUPUESTO", { font: { bold: true, size: 8 }, alignment: { horizontal: "center" } });

    return wb;
  }

  window.cotizadorExcel = {
    construir: construir,
    descargar: async function (d) {
      const wb = await construir(d);
      const buf = await wb.xlsx.writeBuffer();
      descargarBuffer(buf, (d.archivo || "Cotizacion") + ".xlsx");
    }
  };
})();
