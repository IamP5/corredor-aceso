(function () {
  var raiz = document.documentElement;
  var botao = document.querySelector('.tema-botao');
  function rotulo() {
    var escuro = raiz.getAttribute('data-tema') === 'escuro';
    botao.textContent = escuro ? 'Tema claro' : 'Tema escuro';
    botao.setAttribute('aria-label', escuro ? 'Mudar para o tema claro' : 'Mudar para o tema escuro');
  }
  function aplicar(tema, guardar) {
    raiz.setAttribute('data-tema', tema);
    if (guardar) { try { localStorage.setItem('tema', tema); } catch (e) {} }
    rotulo();
  }
  botao.addEventListener('click', function () {
    aplicar(raiz.getAttribute('data-tema') === 'escuro' ? 'claro' : 'escuro', true);
  });
  var preferencia = matchMedia('(prefers-color-scheme: dark)');
  preferencia.addEventListener('change', function (e) {
    var guardado = null;
    try { guardado = localStorage.getItem('tema'); } catch (err) {}
    if (!guardado) aplicar(e.matches ? 'escuro' : 'claro', false);
  });
  rotulo();

  // Largura útil sem a barra de rolagem, para o diagrama sair da coluna sem passar da janela.
  function largura() { raiz.style.setProperty('--vw', raiz.clientWidth + 'px'); }
  largura();
  addEventListener('resize', largura);

  if (!window.pele) return;
  var zooms = [];
  function marcar() {
    zooms.forEach(function (z) { z.figura.classList.toggle('com-zoom', z.zoom.fit < 0.999); });
  }
  document.querySelectorAll('.diagrama-quadro').forEach(function (quadro) {
    var zoom = pele.enableZoom(quadro, { maxScale: 3, labels: { zoomIn: 'Ampliar', zoomOut: 'Reduzir', reset: 'Ver o diagrama inteiro' } });
    zooms.push({ zoom: zoom, figura: quadro.parentNode });
  });
  marcar();
  addEventListener('resize', function () { requestAnimationFrame(marcar); });
  addEventListener('beforeprint', function () { zooms.forEach(function (z) { z.zoom.reset(); }); });
})();
