-- arquivo: sqlite/radar_na_tela.sql
SELECT c.lista, c.ordem, c.casa_token, c.situacao, c.motivo, c.desde,
	c.perfil_contato, c.eletrodependente, c.ultimo_pacote_em, c.saida_avisada,
	n.nome, n.endereco, n.vizinho_nome, n.vizinho_telefone,
	d.desfecho AS ultimo_desfecho, d.ocorrida_em AS desfecho_em, d.estado AS estado_do_desfecho,
	a.registrado_em AS vizinho_avisado_em, a.resposta AS resposta_do_vizinho
FROM radar_card c
LEFT JOIN lista_de_nomes n ON n.casa_token = c.casa_token
LEFT JOIN desfecho d ON d.id = (
	SELECT id FROM desfecho
	WHERE casa_token = c.casa_token
	ORDER BY ocorrida_em DESC LIMIT 1)
LEFT JOIN aviso a ON a.casa_token = c.casa_token AND a.motivo = c.motivo AND a.desde = c.desde
ORDER BY CASE c.lista WHEN 'pendente' THEN 0 WHEN 'atendida' THEN 1 ELSE 2 END, c.ordem;
