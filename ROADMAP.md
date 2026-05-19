# 🗺️ MantisBT MCP Roadmap

Este arquivo serve como o repositório central para discussões arquiteturais, sugestões de melhorias e o planejamento da **Fase 2** do servidor MCP.

## 📌 Tópicos em Discussão

### 1. Robustez e Conectividade
- **Políticas de Retry:** Implementação de resiliência com Polly para lidar com instabilidades no SOAP.
- **Health Checks:** Ferramenta para validar token e URL (`CheckConnectivity`).

### 2. Expansão de Ferramentas (Baseado no SOAP API)
- ✅ **Gestão de Anexos:** Upload (`CreateAttachment`) e download (`GetAttachment`) de arquivos.
- ✅ **Filtros Customizados:** Listagem (`GetFilters`) e execução (`GetIssuesByFilter`) de filtros salvos.
- **Gestão de Tags:** Adicionar/Remover tags de issues para melhor organização.

### 3. Melhorias de Experiência (UX/AI)
- **Resources:** Exposição de logs de erro e estatísticas do projeto como recursos MCP.
- **Busca Semântica Local:** Cache em SQLite para buscas complexas offline.

---
*Última atualização: 18 de Maio de 2026*
