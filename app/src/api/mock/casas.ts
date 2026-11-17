// Gerado por ferramentas/gerar-casas-do-mock.ts a partir da semente simulada.
// Nomes, endereços e telefones são inventados. O DDD 00 não existe.
import type { PerfilDaCasa } from '../contratos';

export interface CasaDoMock {
  n: number;
  casaToken: string;
  nome: string;
  endereco: string;
  perfil: PerfilDaCasa;
  vizinho: { nome: string; telefone: string } | null;
}

export const MICROAREA_DO_MOCK = 3;

export const CASAS_DO_MOCK: readonly CasaDoMock[] = [
  {
    "n": 1,
    "casaToken": "5bf0fa04f41fa9e35ce2450a9f7ca68c",
    "nome": "Efigênia Jatobá",
    "endereco": "Rua do Sabiá, 12 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": true
    },
    "vizinho": {
      "nome": "Odete Embaúba",
      "telefone": "(00) 90000-0001"
    }
  },
  {
    "n": 2,
    "casaToken": "e7dcba79752d871f0f828493edc6bf28",
    "nome": "Neusa Ipê",
    "endereco": "Rua do Bem-te-vi, 14 - Vila Exemplo",
    "perfil": {
      "contato": "Acamado",
      "eletrodependente": true
    },
    "vizinho": {
      "nome": "Raimunda Araucária",
      "telefone": "(00) 90000-0002"
    }
  },
  {
    "n": 3,
    "casaToken": "37729a0c0f979bffcd22df6bc1e8c10a",
    "nome": "Terezinha Aroeira",
    "endereco": "Rua do Tico-tico, 16 - Vila Exemplo",
    "perfil": {
      "contato": "Coabitado",
      "eletrodependente": true
    },
    "vizinho": {
      "nome": "Geraldo Cambuci",
      "telefone": "(00) 90000-0003"
    }
  },
  {
    "n": 4,
    "casaToken": "858ea75ee19ec0015003fd94cb079675",
    "nome": "Geraldo Jequitibá",
    "endereco": "Rua da Saíra, 18 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 5,
    "casaToken": "4067fffaca72b2306398b00db04dd72e",
    "nome": "Madalena Paineira",
    "endereco": "Travessa do João-de-barro, 20 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Palmira Pitangueira",
      "telefone": "(00) 90000-0005"
    }
  },
  {
    "n": 6,
    "casaToken": "ed756120de7a7f5b8305f51b75dce5af",
    "nome": "Iracema Sibipiruna",
    "endereco": "Travessa da Cambacica, 22 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Conceição Jabuticabeira",
      "telefone": "(00) 90000-0006"
    }
  },
  {
    "n": 7,
    "casaToken": "55e874340a08dff0fce65e9ddd65e90f",
    "nome": "Benedito Quaresmeira",
    "endereco": "Viela do Sanhaço, 24 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Zacarias Sapucaia",
      "telefone": "(00) 90000-0007"
    }
  },
  {
    "n": 8,
    "casaToken": "31ee7f8b17d69ff7c3bfd5eaa89614da",
    "nome": "Zulmira Manacá",
    "endereco": "Viela da Corruíra, 26 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 9,
    "casaToken": "8c3b23767b9b61455bb8d3cd0b138a3f",
    "nome": "Francisco Embaúba",
    "endereco": "Rua do Sabiá, 28 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 10,
    "casaToken": "2b51104ba509b34f39931635a37f4854",
    "nome": "Conceição Araucária",
    "endereco": "Rua do Bem-te-vi, 30 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Ivo Tipuana",
      "telefone": "(00) 90000-0010"
    }
  },
  {
    "n": 11,
    "casaToken": "8c0981c6a5d8249a3151e1b8330265b1",
    "nome": "Palmira Cambuci",
    "endereco": "Rua do Tico-tico, 32 - Vila Exemplo",
    "perfil": {
      "contato": "Acamado",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Expedito Mulungu",
      "telefone": "(00) 90000-0011"
    }
  },
  {
    "n": 12,
    "casaToken": "f788ccc577f20e5a7b6cfafe25a2ac92",
    "nome": "Helena Guapuruvu",
    "endereco": "Rua da Saíra, 34 - Vila Exemplo",
    "perfil": {
      "contato": "Coabitado",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 13,
    "casaToken": "15dbaa3a31d79a1035bf0370c765f7f0",
    "nome": "Sebastião Pitangueira",
    "endereco": "Travessa do João-de-barro, 36 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Severina Jatobá",
      "telefone": "(00) 90000-0013"
    }
  },
  {
    "n": 14,
    "casaToken": "7ff9336eb6d307930a649ffa276f3583",
    "nome": "Manoel Jabuticabeira",
    "endereco": "Travessa da Cambacica, 38 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Quitéria Ipê",
      "telefone": "(00) 90000-0014"
    }
  },
  {
    "n": 15,
    "casaToken": "b6bd412ea120004a0f22a08fd7d15944",
    "nome": "Expedito Sapucaia",
    "endereco": "Viela do Sanhaço, 40 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "João Aroeira",
      "telefone": "(00) 90000-0015"
    }
  },
  {
    "n": 16,
    "casaToken": "00cc330cac1a22cbe3afe25d3b2ddbe6",
    "nome": "Ivo Copaíba",
    "endereco": "Viela da Corruíra, 42 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 17,
    "casaToken": "01f294e254d08cfd5e2fbc9add699063",
    "nome": "Geralda Jacarandá",
    "endereco": "Rua do Sabiá, 44 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Lázaro Paineira",
      "telefone": "(00) 90000-0017"
    }
  },
  {
    "n": 18,
    "casaToken": "e01011a482b415cc97dffea893b665f1",
    "nome": "Lázaro Tipuana",
    "endereco": "Rua do Bem-te-vi, 46 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Aparecida Sibipiruna",
      "telefone": "(00) 90000-0018"
    }
  },
  {
    "n": 19,
    "casaToken": "2a3a1fb23de4e2fe75e9c4dd2ac61ca4",
    "nome": "Djalma Mulungu",
    "endereco": "Rua do Tico-tico, 48 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 20,
    "casaToken": "41283f3d5e3b8e0145506fc6b4b70931",
    "nome": "Lourdes Grumixama",
    "endereco": "Rua da Saíra, 50 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 21,
    "casaToken": "671a67b021a8d5bf408ba576d2fe13a9",
    "nome": "Pedro Jatobá",
    "endereco": "Travessa do João-de-barro, 52 - Vila Exemplo",
    "perfil": {
      "contato": "Acamado",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Orlando Embaúba",
      "telefone": "(00) 90000-0021"
    }
  },
  {
    "n": 22,
    "casaToken": "9feed1bdd59e5dbb33939261cb42fde8",
    "nome": "Quitéria Ipê",
    "endereco": "Travessa da Cambacica, 54 - Vila Exemplo",
    "perfil": {
      "contato": "Coabitado",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Josefa Araucária",
      "telefone": "(00) 90000-0022"
    }
  },
  {
    "n": 23,
    "casaToken": "2b90eac2d9f8b1b8cbbd98e515b676ae",
    "nome": "Zacarias Aroeira",
    "endereco": "Viela do Sanhaço, 56 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Dirce Cambuci",
      "telefone": "(00) 90000-0023"
    }
  },
  {
    "n": 24,
    "casaToken": "c569c3affe85f17f8cd03daf1213ae0c",
    "nome": "Aparecida Jequitibá",
    "endereco": "Viela da Corruíra, 58 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 25,
    "casaToken": "49a209105b47acb319fe7757d21cdf2c",
    "nome": "Josefa Paineira",
    "endereco": "Rua do Sabiá, 60 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Manoel Pitangueira",
      "telefone": "(00) 90000-0025"
    }
  },
  {
    "n": 26,
    "casaToken": "9aa0dbd68846fce96d6598d4efc49cbb",
    "nome": "Valdemar Sibipiruna",
    "endereco": "Rua do Bem-te-vi, 62 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Raimunda Jabuticabeira",
      "telefone": "(00) 90000-0026"
    }
  },
  {
    "n": 27,
    "casaToken": "ca1200cd5d59d00017c0ceb9a013051a",
    "nome": "Francisca Quaresmeira",
    "endereco": "Rua do Tico-tico, 64 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "João Sapucaia",
      "telefone": "(00) 90000-0027"
    }
  },
  {
    "n": 28,
    "casaToken": "a1a364ce0792d3d0dfa9a4cb6812570f",
    "nome": "Orlando Manacá",
    "endereco": "Rua da Saíra, 66 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 29,
    "casaToken": "950ba862fd916b707c353628a1451bab",
    "nome": "Antônio Embaúba",
    "endereco": "Travessa do João-de-barro, 68 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 30,
    "casaToken": "296e070effd4af1011cd85b9c15dc720",
    "nome": "Raimundo Araucária",
    "endereco": "Travessa da Cambacica, 70 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Zacarias Tipuana",
      "telefone": "(00) 90000-0030"
    }
  },
  {
    "n": 31,
    "casaToken": "b957bfa74e9bd6249ce21fbc1048626b",
    "nome": "Valdete Cambuci",
    "endereco": "Viela do Sanhaço, 72 - Vila Exemplo",
    "perfil": {
      "contato": "Acamado",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Palmira Mulungu",
      "telefone": "(00) 90000-0031"
    }
  },
  {
    "n": 32,
    "casaToken": "74bccee6abe9c39185af9a68bf9d331e",
    "nome": "Severina Guapuruvu",
    "endereco": "Viela da Corruíra, 74 - Vila Exemplo",
    "perfil": {
      "contato": "Coabitado",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 33,
    "casaToken": "43d12870bb490b1977df87ab122cdf60",
    "nome": "Dirce Pitangueira",
    "endereco": "Rua do Sabiá, 76 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Valdemar Jatobá",
      "telefone": "(00) 90000-0033"
    }
  },
  {
    "n": 34,
    "casaToken": "2de2a780e97c2a8168f5c620badb5e95",
    "nome": "Cícero Jabuticabeira",
    "endereco": "Rua do Bem-te-vi, 78 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Dirce Ipê",
      "telefone": "(00) 90000-0034"
    }
  },
  {
    "n": 35,
    "casaToken": "558a4d0f69657d925bfe37014e923d7f",
    "nome": "Hélio Sapucaia",
    "endereco": "Rua do Tico-tico, 80 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Valdete Aroeira",
      "telefone": "(00) 90000-0035"
    }
  },
  {
    "n": 36,
    "casaToken": "2062a3c0751035fd13bfcc51e26d3e57",
    "nome": "Raimunda Copaíba",
    "endereco": "Rua da Saíra, 82 - Vila Exemplo",
    "perfil": {
      "contato": "Coabitado",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 37,
    "casaToken": "7591154b79caf0c45a075fa44e666491",
    "nome": "Benedita Jacarandá",
    "endereco": "Travessa do João-de-barro, 84 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Conceição Paineira",
      "telefone": "(00) 90000-0037"
    }
  },
  {
    "n": 38,
    "casaToken": "048d7564db8c13db3c0079e252c616e1",
    "nome": "Odete Tipuana",
    "endereco": "Travessa da Cambacica, 86 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": {
      "nome": "Madalena Sibipiruna",
      "telefone": "(00) 90000-0038"
    }
  },
  {
    "n": 39,
    "casaToken": "57f6c7d204f7be06fc1e801b907dde56",
    "nome": "Nelson Mulungu",
    "endereco": "Viela do Sanhaço, 88 - Vila Exemplo",
    "perfil": {
      "contato": "Coabitado",
      "eletrodependente": false
    },
    "vizinho": null
  },
  {
    "n": 40,
    "casaToken": "d3a444a38667bbb0dc4516d777dc8c56",
    "nome": "João Grumixama",
    "endereco": "Viela da Corruíra, 90 - Vila Exemplo",
    "perfil": {
      "contato": "Sozinho",
      "eletrodependente": false
    },
    "vizinho": null
  }
];
