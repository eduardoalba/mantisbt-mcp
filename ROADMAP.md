# 🗺️ MantisBT MCP Roadmap

Este arquivo serve como o repositório central para discussões arquiteturais, sugestões de melhorias e o planejamento da **Fase 2** do servidor MCP.

## 📌 Tópicos em Discussão

### 1. Robustez e Conectividade
- **Políticas de Retry:** Implementação de resiliência com Polly para lidar com instabilidades no SOAP.
- **Health Checks:** Ferramenta para validar token e URL (`CheckConnectivity`).

### 2. Expansão de Ferramentas (Baseado no SOAP API)
- ✅ **Gestão de Anexos de Issue:** Upload (`CreateAttachment`) e download (`GetAttachment`) de arquivos.
- ✅ **Gestão de Anexos de Projeto:** Upload (`CreateProjectAttachment`), download (`GetProjectAttachment`) e listagem (`GetProjectAttachments`).
- ✅ **Filtros Customizados:** Listagem (`GetFilters`) e execução (`GetIssuesByFilter`) de filtros salvos.
- ✅ **Gestão de Tags:** Listagem (`GetTags`) e associação (`SetIssueTags`) de tags em issues.
- ✅ **Gestão de Projetos Avançada:** Listagem de usuários do projeto (`GetProjectUsers`), subprojetos (`GetSubprojects`) e Custom Fields (`GetProjectCustomFields`).
- ✅ **Busca Avançada:** Busca por resumo exato (`SearchIssueBySummary`).
- ✅ **Enums Expandidos:** Inclusão de Access Levels, Project Status e Reproducibility em `GetSystemEnums`.
- **Gestão de Versões:** CRUD de versões de projeto para suporte a roadmaps de release.

### 3. Melhorias de Experiência (UX/AI)
- ✅ **Resources:** Exposição de logs de erro e estatísticas do projeto como recursos MCP.
- ✅ **Busca Semântica Local:** Cache em SQLite para buscas complexas offline (via `sync_project` e `search_semantic`).

---
*Última atualização: Maio de 2026*
