import { criarApiHttp, ErroHttp } from '../api/cliente';
import { lerCodigoDeAcesso } from '../estado/sessao';

// A API só aceita o token da ACS em `Authorization: Bearer` (api/README.md, "Autenticação").
// Antes da integração o app mandava o código digitado em minúsculas, e a API real respondia 401.
describe('cliente HTTP contra a API real', () => {
  const pedidos: { url: string; init: RequestInit }[] = [];
  const fetchOriginal = global.fetch;

  beforeEach(() => {
    pedidos.length = 0;
    global.fetch = jest.fn(async (url: string, init: RequestInit) => {
      pedidos.push({ url, init });
      return new Response(JSON.stringify({ turnoAberto: false, agora: '2026-11-18T10:31:00-03:00', pendentes: [], atendidas: [], aguardando: [], verdes: 0 }));
    }) as unknown as typeof fetch;
  });

  afterAll(() => {
    global.fetch = fetchOriginal;
  });

  it('manda o código de acesso como está, no cabeçalho Bearer, na rota /api', async () => {
    const api = criarApiHttp('http://localhost:5080/', () => 'Dev-ACS-Token-01');

    await api.radar(3);

    expect(pedidos[0].url).toBe('http://localhost:5080/api/radar?microarea=3');
    expect((pedidos[0].init.headers as Record<string, string>).Authorization).toBe('Bearer Dev-ACS-Token-01');
  });

  it('encerra o turno pedindo o resumo da microárea da ACS, como o radar', async () => {
    global.fetch = jest.fn(async (url: string, init: RequestInit) => {
      pedidos.push({ url, init });
      return new Response(JSON.stringify({
        data: '2026-11-18',
        encerradoEm: '2026-11-18T17:30:00-03:00',
        resumo: { casasAtivas: 40, comSinalDeVidaAte10h: 24, atendidas: 4, vermelhasPendentes: 1, cinzasPendentes: 1 },
      }));
    }) as unknown as typeof fetch;
    const api = criarApiHttp('http://localhost:5080', () => 'Dev-ACS-Token-01');

    const encerramento = await api.encerrarTurno('2026-11-18', 3);

    expect(pedidos[0].url).toBe('http://localhost:5080/api/dias-criticos/2026-11-18/encerramento?microarea=3');
    expect(encerramento.resumo.casasAtivas).toBe(40);
  });

  it('sem sessão não manda cabeçalho, e o 401 chega como ErroHttp', async () => {
    global.fetch = jest.fn(async () => new Response('', { status: 401 })) as unknown as typeof fetch;
    const api = criarApiHttp('http://localhost:5080', () => null);

    await expect(api.radar(3)).rejects.toEqual(new ErroHttp(401, 'HTTP_401'));
  });
});

describe('código de acesso digitado na entrada', () => {
  it('guarda o token de desenvolvimento da API sem mudar nada', () => {
    expect(lerCodigoDeAcesso('  dev-acs-demo-01-so-no-notebook ')).toBe('dev-acs-demo-01-so-no-notebook');
  });

  it('mantém maiúsculas, porque a API compara byte a byte', () => {
    expect(lerCodigoDeAcesso('AbC-123')).toBe('AbC-123');
  });

  it('recusa vazio, curto demais e com espaço no meio', () => {
    expect(lerCodigoDeAcesso('   ')).toBeNull();
    expect(lerCodigoDeAcesso('ab')).toBeNull();
    expect(lerCodigoDeAcesso('dois codigos')).toBeNull();
  });
});
