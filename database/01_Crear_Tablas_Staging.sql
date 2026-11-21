-- ============================================================================
-- PROYECTO: Sistema de Análisis de Opiniones de Clientes (ETL)
-- ACTIVIDAD 1: Extracción y Arquitectura del Worker Service
-- BASE DE DATOS: OpinionsBd
-- ESTUDIANTE: Wilmer José Torres Andeliz | DOCENTE: Francis Ramírez
-- INSTITUCIÓN: Instituto Tecnológico de Las Américas (ITLA)
-- ============================================================================

USE [OpinionsBd];
GO

-- 1. Staging para Encuestas Internas (CSV)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'stg_Encuestas_Csv')
BEGIN
    CREATE TABLE [dbo].[stg_Encuestas_Csv](
        [IdStaging]           INT IDENTITY(1,1) PRIMARY KEY,
        [IdOpinion]           INT NOT NULL,
        [IdCliente]           INT NULL,
        [IdProducto]          INT NULL,
        [Fecha]               DATETIME NULL,
        [Comentario]          VARCHAR(MAX) NULL,
        [Clasificacion]       VARCHAR(50) NULL,
        [PuntajeSatisfaccion] INT NULL,
        [Fuente]              VARCHAR(50) NULL,
        [FechaExtraccion]     DATETIME DEFAULT GETDATE()
    );
    PRINT '>> Tabla staging stg_Encuestas_Csv lista.';
END
GO

-- 2. Staging para Reseñas Web (SQL Server)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'stg_Resenas_Database')
BEGIN
    CREATE TABLE [dbo].[stg_Resenas_Database](
        [IdStaging]           INT IDENTITY(1,1) PRIMARY KEY,
        [IdReview]            VARCHAR(50) NOT NULL,
        [IdCliente]           VARCHAR(50) NULL,
        [IdProducto]          VARCHAR(50) NULL,
        [Fecha]               DATETIME NULL,
        [Comentario]          VARCHAR(MAX) NULL,
        [Rating]              INT NULL,
        [FechaExtraccion]     DATETIME DEFAULT GETDATE()
    );
    PRINT '>> Tabla staging stg_Resenas_Database lista.';
END
GO

-- 3. Staging para Comentarios de Redes Sociales (API REST)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'stg_Comentarios_Api')
BEGIN
    CREATE TABLE [dbo].[stg_Comentarios_Api](
        [IdStaging]           INT IDENTITY(1,1) PRIMARY KEY,
        [IdComment]           VARCHAR(50) NOT NULL,
        [IdCliente]           VARCHAR(50) NULL,
        [IdProducto]          VARCHAR(50) NULL,
        [Fuente]              VARCHAR(50) NULL,
        [Fecha]               DATETIME NULL,
        [Comentario]          VARCHAR(MAX) NULL,
        [FechaExtraccion]     DATETIME DEFAULT GETDATE()
    );
    PRINT '>> Tabla staging stg_Comentarios_Api lista.';
END
GO
