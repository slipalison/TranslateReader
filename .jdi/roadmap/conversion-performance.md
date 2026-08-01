---
order: 16
name: Validacao funcional e performance da conversao
---
- **Slug:** conversion-performance
- **Goal:** provar por teste que conversao de livro, extracao de imagens e download de modelo funcionam de ponta a ponta em livro CURTO e em livro GRANDE (fixtures de 1,7 MB / 27 capitulos e 32 MB / 256 imagens ja no repo), e corrigir os gargalos nomeados que a validacao expuser — comecando pelo `ExtractAllImagesAsync`, que hoje materializa 44 MB de imagens num unico dicionario (229 alocacoes na LOH), contra `.claude/rules/csharp.md` §2.3
