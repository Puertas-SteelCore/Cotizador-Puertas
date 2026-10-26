(function () {
  const archivos = {
    sanimax: "img/sanimax.jpg",
    modumex: "img/puertas-modumex.png",
    steelcore: "img/steelcore.png",
    pie: "img/pie.png",
    corte: "img/puerta-corte.jpg",
    acabado: "img/resistencia-colores.png"
  };
  let cache = null;

  function leer(url) {
    return fetch(url, { cache: "force-cache" })
      .then(function (r) { if (!r.ok) throw new Error(url); return r.blob(); })
      .then(function (b) {
        return new Promise(function (ok, mal) {
          const fr = new FileReader();
          fr.onload = function () {
            const img = new Image();
            img.onload = function () {
              ok({ data: fr.result, w: img.naturalWidth, h: img.naturalHeight, tipo: b.type.indexOf("png") >= 0 ? "PNG" : "JPEG" });
            };
            img.onerror = mal;
            img.src = fr.result;
          };
          fr.onerror = mal;
          fr.readAsDataURL(b);
        });
      })
      .catch(function () { return null; });
  }

  window.cotizadorImagenes = {
    desdeData: function (data) {
      return new Promise(function (ok) {
        const img = new Image();
        img.onload = function () { ok({ data: data, w: img.naturalWidth, h: img.naturalHeight, tipo: data.indexOf("image/png") >= 0 ? "PNG" : "JPEG" }); };
        img.onerror = function () { ok(null); };
        img.src = data;
      });
    },
    cargar: async function () {
      if (cache) return cache;
      const r = {};
      const claves = Object.keys(archivos);
      const datos = await Promise.all(claves.map(function (k) { return leer(archivos[k]); }));
      claves.forEach(function (k, i) { r[k] = datos[i]; });
      cache = r;
      return r;
    }
  };
})();
