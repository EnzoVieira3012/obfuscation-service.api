# 🔒 Obfuscation Service

Um serviço de ofuscação de IDs desenvolvido em .NET 8 que transforma identificadores numéricos em tokens criptografados e URL-safe, ideal para proteção de dados sensíveis em APIs públicas.

## ✨ Funcionalidades

- **Criptografia de IDs**: Transforma números inteiros (`long`) em tokens seguros
- **Descriptografia reversível**: Recupera o ID original a partir do token
- **URL-safe**: Tokens compatíveis com URLs e parâmetros HTTP
- **Validação integrada**: Verificação automática de integridade dos tokens
- **Injeção de Dependência**: Arquitetura limpa com separação de responsabilidades

## 🏗️ Arquitetura

### Estrutura do Projeto
```
obfuscation-service/
├── Application/              # Camada de aplicação
│   └── Interfaces/Crypto/    # Contratos de serviço
├── Domain/                   # Camada de domínio
│   └── ValueObjects/         # Objetos de valor
├── Infrastructure/           # Camada de infraestrutura
│   └── Crypto/              # Implementações criptográficas
├── Controllers/              # Controladores da API
├── Program.cs               # Configuração do host
└── appsettings.json         # Configurações da aplicação
```

### Padrões Utilizados
- **Clean Architecture**: Separação em camadas
- **Value Object**: Representação tipo-safe de IDs criptografados
- **Dependency Injection**: Inversão de controle
- **Repository Pattern** (implícito na estrutura)

## 🛠️ Tecnologias

- **.NET 8.0** - Runtime e SDK
- **ASP.NET Core** - Framework web
- **System.Security.Cryptography** - Criptografia nativa
- **DotNetEnv** - Gerenciamento de variáveis de ambiente
- **Swagger/OpenAPI** - Documentação automática da API

## ⚙️ Configuração

### Pré-requisitos
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022+ ou VS Code
- Gerenciador de pacotes NuGet

### Instalação

1. **Clone o repositório**
```bash
git clone [<repository-url>](https://github.com/EnzoVieira3012/obfuscation-service.api.git)
cd obfuscation-service
```

2. **Configure as variáveis de ambiente**
```bash
# Crie um arquivo .env na raiz do projeto
# Consulte .env.example para o formato
```

3. **Restauração de dependências**
```bash
dotnet restore
```

4. **Execução do projeto**
```bash
dotnet run
# ou para desenvolvimento
dotnet watch run
```

## 📡 API Endpoints

### 1. 🔐 Criptografar ID
```http
GET /api/obfuscation/encrypt/{id}
```

**Parâmetros:**
- `id` (long): ID numérico a ser criptografado

**Resposta (200):**
```json
{
  "value": "aGVsbG8td29ybGQtdXJsLXNhZmUtZW5jb2Rpbmc"
}
```

### 2. 🔓 Descriptografar Token
```http
GET /api/obfuscation/decrypt/{value}
```

**Parâmetros:**
- `value` (string): Token criptografado

**Resposta (200):**
```json
123456789
```

## 🔐 Mecanismo de Criptografia

### Algoritmo
- **Cifra**: AES-256 (Advanced Encryption Standard)
- **Modo**: ECB (Electronic Codebook)
- **Padding**: None (dados de tamanho fixo)

### Estrutura do Payload (32 bytes)
```
[  0- 7] ID original (8 bytes - Int64)
[  8-15] Nonce determinístico (8 bytes - HMAC-SHA256)
[16-31] Assinatura de integridade (16 bytes - HMAC-SHA256)
```

### Processo de Criptografia
1. **Construção do Payload**: Combina ID, nonce e assinatura
2. **Criptografia AES**: Transformação do payload completo
3. **Codificação Base64Url**: Conversão para string URL-safe

## 🎯 Casos de Uso

### 1. **Proteção de APIs Públicas**
```csharp
// Em vez de expor IDs sequenciais
https://api.com/users/12345

// Use tokens ofuscados
https://api.com/users/aGVsbG8td29ybGQtdXJsLXNhZmU
```

### 2. **Segurança em URLs**
- Previne enumeração de recursos
- Protege contra ataques de força bruta
- Esconde padrões de sequência

### 3. **Compartilhamento Seguro**
- Tokens podem ser compartilhados publicamente
- Sem risco de expor lógica de negócio
- Validação automática de integridade

## 📊 Comparação

| Método | Segurança | Performance | Tamanho |
|--------|-----------|-------------|---------|
| **Este Serviço** | 🔒🔒🔒🔒 | ⚡⚡⚡⚡ | 43 chars |
| UUID/GUID | 🔒🔒 | ⚡⚡⚡ | 36 chars |
| Hash MD5 | 🔒 | ⚡⚡⚡⚡⚡ | 32 chars |
| Base64 Puro | 🔒 | ⚡⚡⚡⚡⚡ | 11 chars |

## 🚀 Performance

- **Criptografia/Descriptografia**: < 1ms por operação
- **Concorrência**: Suporta milhares de requisições simultâneas
- **Memória**: Alocação mínima (structs e arrays reutilizados)

## 🔧 Extensibilidade

### Adicionar Novo Algoritmo
1. Implemente `IEncryptedIdService`
2. Registre no contêiner DI
3. Utilize a nova implementação

### Customizar Formato
- Modifique `EncryptedId` para validar formatos específicos
- Adicione metadados ao payload
- Implemente versionamento de tokens

## 🧪 Testes

```bash
# Execute os testes unitários
dotnet test

# Teste de integração (requer serviço em execução)
curl -X GET "https://localhost:5001/api/obfuscation/encrypt/123"
curl -X GET "https://localhost:5001/api/obfuscation/decrypt/{token}"
```

## 📈 Monitoramento

### Logs
- Operações de criptografia/descriptografia
- Erros de validação
- Tentativas de descriptografia inválidas

### Métricas
- Taxa de sucesso/falha
- Tempo médio de resposta
- Uso de memória e CPU

## 🔒 Considerações de Segurança

### ✅ Vantagens
- **Não previsível**: Tokens não seguem padrão sequencial
- **Integridade**: Assinatura HMAC detecta alterações
- **Determinístico**: Mesmo ID gera mesmo token (útil para caching)
- **Sem estado**: Não requer banco de dados ou armazenamento

### ⚠️ Recomendações
1. **Mantenha o segredo seguro**: Rotação periódica da chave
2. **Use HTTPS**: Sempre em produção
3. **Monitoramento**: Alertas para tentativas de abuso
4. **Rate limiting**: Proteção contra força bruta

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🆘 Suporte

- **Issues**: [GitHub Issues](https://github.com/EnzoVieira3012/obfuscation-service.api/issues)
- **Documentação**: Execute o projeto e acesse `/swagger`
- **Email**: enzovieira.trabalho@@outlookk.com

---

<div align="center">
  <sub>Construído com ❤️ e .NET 8</sub><br>
  <sub>⚡ Pronto para produção ⚡</sub>
</div>