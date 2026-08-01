---
order: 12
name: Rede de testes de regressao
---
- **Slug:** regression-suite
- **Goal:** fixar o comportamento observavel de hoje em testes de caracterizacao, para que qualquer alteracao futura (em especial o refactor da phase `the-method-refactor`) quebre um teste em vez de quebrar o app — cobrindo os caminhos do Core hoje sem teste, e decidindo explicitamente o que fazer com as 1516 linhas do projeto MAUI que o test project atual nao alcanca
