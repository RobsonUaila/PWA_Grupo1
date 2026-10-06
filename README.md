# AI Soil Moisture Drought Classification System — Grupo 1

Aplicação web em ASP.NET Core (.NET 10) desenvolvida para o Projecto 1 da disciplina de
Programação Web Avançada (.NET), USTM — monitoria e classificação da condição
de seca em **Chókwè** (Gaza) e **Massinga** (Inhambane), com base em humidade do solo (Sentinel-1,
ASCAT) e propriedades do solo (SoilGrids), utilizando **ML.NET**.

---

## Arquitetura da Solução

```
Drought.Web            → ASP.NET Core MVC (Portal Grupo 1, Dashboard, Mapa Leaflet, Autenticação, Administração)
Drought.Api            → ASP.NET Core Web API (REST + Swagger OpenAPI)
Drought.Application    → Casos de uso e serviços (PredictionService, TrainingService)
Drought.Domain         → Entidades de domínio (Region, SoilObservation, DatasetSource, Prediction, ModelVersion, ApplicationUser)
Drought.Infrastructure → EF Core (DroughtDbContext, Migrations), importação CSV
Drought.ML             → Pipeline de Machine Learning ML.NET (SDCA & LightGBM)
Drought.Tests          → Testes unitários automatizados (xUnit)
data/                  → Dataset multi-fonte (180 observações Sentinel-1, ASCAT, SoilGrids)
docs/                  → Documentação técnica detalhada
```

---

## Funcionalidades Implementadas

1. **Portal Principal (Grupo 1):** Indicadores em tempo real, resumo analítico das regiões e critérios hidrológicos de seca.
2. **Dashboard Interativo & Mapa (Desafio 2.8):** 
   - **Mapa Geográfico Leaflet.js:** Monitoria georreferenciada para Chókwè e Massinga com cores dinâmicas por nível de seca.
   - **Séries Temporais Chart.js:** Comparativo de humidade superficial Sentinel-1 vs ASCAT.
   - **Simulador de Previsão por IA em Tempo Real:** Permite testar o modelo ML.NET diretamente no ecrã.
3. **Módulo de Machine Learning (ML.NET):**
   - Comparação concorrente de algoritmos: **SDCA** (Maximum Entropy) e **LightGBM** (FastTree).
   - Cálculo e persistência de métricas (MacroAccuracy, MicroAccuracy, LogLoss) com ativação automática do melhor modelo.
4. **Módulo de Administração:** Upload e validação estrita de datasets CSV e re-treino de modelos.
5. **Autenticação e Autorização (ASP.NET Core Identity):**
   - Perfis: `Administrador` e `Investigador`.
   - Contas de demonstração pré-configuradas:
     - **Admin:** `admin@drought.mz` / `Admin123!`
     - **Investigador:** `investigador@drought.mz` / `Invest123!`
6. **API REST com Swagger:** Endpoints documentados para importação, treino, histórico e previsões pontuais.

---

## Como Correr o Projeto

### 1. Iniciar a Base de Dados (Docker PostgreSQL)
```powershell
docker compose up -d
```

### 2. Executar no Visual Studio Community
1. Abra a solução `Drought.sln`.
2. Clique com botão direito na solução → **Configure Startup Projects...**
3. Selecione **Multiple startup projects** e defina `Drought.Api` e `Drought.Web` como **Start**.
4. Pressione **F5** (ou **Ctrl + F5**).

* **Portal Web:** `http://localhost:5002` ou `https://localhost:5003`
* **Swagger API:** `http://localhost:5000/swagger` ou `https://localhost:5001/swagger`

---

## Testes Automatizados

Para executar os testes unitários da camada de importação e validação:
```powershell
cd Drought.Tests
dotnet test
```

---

## Grupo 1 — USTM
Desenvolvido no âmbito da Licenciatura em Engenharia Informática / Desenvolvimento de Software na **Universidade São Tomás de Moçambique (USTM)**.
Inspirado na iniciativa científica **AI4Drought-MZ**.
