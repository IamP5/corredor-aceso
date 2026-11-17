# Chaves públicas

Só arquivos `.pub` entram aqui. O `.gitignore` recusa qualquer outro arquivo.

- `equipe/<nome>.pub`: uma chave por pessoa. O Terraform põe todas na VM na criação e o `01-rede.sh` mantém a lista em dia. Apagar o arquivo e provisionar de novo tira o acesso.
- `deploy.pub`: chave do GitHub Actions. A metade privada existe só no segredo `VM_CHAVE_DEPLOY` do ambiente `producao`.
