# Diagrama de Arquitectura — Sistema de Análisis de Opiniones de Clientes

**Estudiante:** Wilmer José Torres Andeliz  
**Asignatura:** Electiva I — Prof. Francis Ramírez  
**Institución:** Instituto Tecnológico de Las Américas (ITLA)  
**Fecha:** Noviembre de 2026  

---

## 🏛️ Diagrama Conceptual del Flujo de Extracción

```mermaid
graph TD
    subgraph Fuentes ["1. Orígenes Heterogéneos"]
        F1["📄 Encuestas Internas<br/>(surveys_part1.csv)"]
        F2["🗄️ Reseñas Web<br/>(SQL Server dbo.Web)"]
        F3["🌐 Comentarios Sociales<br/>(API REST)"]
    end

    subgraph Worker ["2. Worker Service (.NET 8) — BackgroundService"]
        subgraph Extractores ["Módulo de Extracción (IExtractor)"]
            E1["CsvExtractor<br/>(CsvHelper)"]
            E2["DatabaseExtractor<br/>(ADO.NET Async)"]
            E3["ApiExtractor<br/>(HttpClientFactory)"]
        end
        Log["LoggerService (ILogger / Stopwatch)"]
        Config["appsettings.json (Opciones y Credenciales)"]
    end

    subgraph Staging ["3. Zona de Almacenamiento Temporal (Staging)"]
        S1[("stg_Encuestas_Csv<br/>SQL Server")]
        S2[("stg_Resenas_Database<br/>SQL Server")]
        S3[("stg_Comentarios_Api<br/>SQL Server")]
        S4["temp_staging/<br/>Respaldos JSON"]
    end

    subgraph Posteriores ["4. Fases Posteriores del Pipeline ETL (Líneas Discontinuas)"]
        Trans["Fase de Transformación (T)<br/>Limpieza, Deduplicación y Sentimiento"]
        DW[("Base Analítica (Data Warehouse)<br/>OpinionsBd (Modelo Estrella)")]
        Dash["Capa de Visualización<br/>Dashboards Power BI / KPIs"]
    end

    F1 --> E1
    F2 --> E2
    F3 --> E3

    E1 --> S1
    E2 --> S2
    E3 --> S3
    E1 -.-> S4
    E2 -.-> S4
    E3 -.-> S4

    S1 -.-> Trans
    S2 -.-> Trans
    S3 -.-> Trans
    Trans -.-> DW
    DW -.-> Dash

    Config -.-> Worker
    Log -.-> Worker
```

### Componentes de la Arquitectura
* **Fuentes de datos:** Tres orígenes con contratos distintos: archivo plano estructurado (CSV), tabla relacional en SQL Server (ADO.NET) y servicio HTTP (API REST).
* **Worker Service (.NET 8):** Servicio en segundo plano (`BackgroundService`) con Inyección de Dependencias, tareas asíncronas y ejecución en paralelo (`Task.WhenAll`).
* **Zona de Staging:** Almacenamiento intermedio en tablas `stg_*` de SQL Server y volcados JSON para desacoplar la extracción de las fases de transformación.
* **Fases Posteriores:** La transformación y carga al modelo dimensional (estrella) se nutren exclusivamente de la zona de staging.
