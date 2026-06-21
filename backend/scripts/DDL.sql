IF DB_ID(N'Loteria') IS NULL
    BEGIN
        CREATE DATABASE [Loteria];
        PRINT 'Created database Loteria';
    END
ELSE
    BEGIN
        PRINT 'Database Loteria already exists';
    END
GO

USE [Loteria];
GO  

-- -- DROP ALL TABLES -------------------------------------------------
-- BEGIN
--     BEGIN TRY
--         BEGIN TRANSACTION;

--         IF OBJECT_ID(N'dbo.ConcursoNumero', N'U') IS NOT NULL
--         BEGIN
--             DROP TABLE dbo.ConcursoNumero;
--             PRINT 'Dropped table dbo.ConcursoNumero';
--         END

--         IF OBJECT_ID(N'dbo.ConcursoPremio', N'U') IS NOT NULL
--         BEGIN
--             DROP TABLE dbo.ConcursoPremio;
--             PRINT 'Dropped table dbo.ConcursoPremio';
--         END

--         IF OBJECT_ID(N'dbo.ConcursoGanhadorLocal', N'U') IS NOT NULL
--         BEGIN
--             DROP TABLE dbo.ConcursoGanhadorLocal;
--             PRINT 'Dropped table dbo.ConcursoGanhadorLocal';
--         END

--         IF OBJECT_ID(N'dbo.LotofacilDetalhe', N'U') IS NOT NULL
--         BEGIN
--             DROP TABLE dbo.LotofacilDetalhe;
--             PRINT 'Dropped table dbo.LotofacilDetalhe';
--         END

--         IF OBJECT_ID(N'dbo.Concurso', N'U') IS NOT NULL
--         BEGIN
--             DROP TABLE dbo.Concurso;
--             PRINT 'Dropped table dbo.Concurso';
--         END

--         IF @@TRANCOUNT > 0
--             COMMIT;

--     END TRY
--     BEGIN CATCH

--         IF @@TRANCOUNT > 0
--             ROLLBACK;

--         SELECT ERROR_NUMBER() AS ErrorNumber;
--         SELECT ERROR_MESSAGE() AS ErrorMessage;

--     END CATCH;
-- END;
-- -- DROP ALL TABLES -------------------------------------------------
-- GO

IF OBJECT_ID(N'dbo.Concurso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Concurso
    (
        Id bigint IDENTITY(1,1) NOT NULL,
        TipoJogo smallint NOT NULL,
        NumeroConcurso int NOT NULL,
        DataApuracao date NULL,
        LocalSorteio nvarchar(120) NULL,
        MunicipioUFSorteio nvarchar(120) NULL,
        CONSTRAINT PK_Concurso PRIMARY KEY (Id)
    );
    
    CREATE UNIQUE INDEX UX_Concurso_Tipo_Numero
        ON dbo.Concurso (TipoJogo, NumeroConcurso);
    
    PRINT 'Created table dbo.Concurso';
END
ELSE
    BEGIN
        PRINT 'Table dbo.Concurso already exists';
    END
GO

IF OBJECT_ID(N'dbo.ConcursoNumero', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConcursoNumero
    (
        Id bigint IDENTITY(1,1) NOT NULL,
        ConcursoId bigint NOT NULL,
        Numero smallint NOT NULL,
        Posicao tinyint NULL,
        CONSTRAINT PK_ConcursoNumero PRIMARY KEY (Id),
        CONSTRAINT FK_ConcursoNumero_Concurso
            FOREIGN KEY (ConcursoId) REFERENCES dbo.Concurso(Id)
                ON DELETE CASCADE
    );
    
    CREATE INDEX IX_ConcursoNumero_ConcursoId
        ON dbo.ConcursoNumero (ConcursoId);
    
    CREATE UNIQUE INDEX UX_ConcursoNumero_Concurso_Numero
        ON dbo.ConcursoNumero (ConcursoId, Numero);
    
    CREATE UNIQUE INDEX UX_ConcursoNumero_Concurso_Posicao
        ON dbo.ConcursoNumero (ConcursoId, Posicao)
        WHERE Posicao IS NOT NULL;
    
    PRINT 'Created table dbo.ConcursoNumero';
END
ELSE
    BEGIN
        PRINT 'Table dbo.ConcursoNumero already exists';
    END
GO

IF OBJECT_ID(N'dbo.ConcursoPremio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConcursoPremio
    (
        Id bigint IDENTITY(1,1) NOT NULL,
        ConcursoId bigint NOT NULL,
        Faixa int NOT NULL,
        DescricaoFaixa nvarchar(80) NULL,
        NumeroDeGanhadores int NOT NULL,
        ValorPremio decimal(18,2) NOT NULL,
        CONSTRAINT PK_ConcursoPremio PRIMARY KEY (Id),
        CONSTRAINT FK_ConcursoPremio_Concurso
            FOREIGN KEY (ConcursoId) REFERENCES dbo.Concurso(Id)
                ON DELETE CASCADE
    );
    
    CREATE INDEX IX_ConcursoPremio_ConcursoId
        ON dbo.ConcursoPremio (ConcursoId);
    
    CREATE UNIQUE INDEX UX_ConcursoPremio_Concurso_Faixa
        ON dbo.ConcursoPremio (ConcursoId, Faixa);
    
    PRINT 'Created table dbo.ConcursoPremio';
END
ELSE
    BEGIN
        PRINT 'Table dbo.ConcursoPremio already exists';
    END
GO

IF OBJECT_ID(N'dbo.ConcursoGanhadorLocal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConcursoGanhadorLocal
    (
        Id bigint IDENTITY(1,1) NOT NULL,
        ConcursoId bigint NOT NULL,
        Quantidade int NOT NULL,
        Municipio nvarchar(120) NULL,
        Uf varchar(2) NOT NULL,
        CONSTRAINT PK_ConcursoGanhadorLocal PRIMARY KEY (Id),
        CONSTRAINT FK_ConcursoGanhadorLocal_Concurso
            FOREIGN KEY (ConcursoId) REFERENCES dbo.Concurso(Id)
                ON DELETE CASCADE
    );
    
    CREATE INDEX IX_ConcursoGanhadorLocal_ConcursoId
        ON dbo.ConcursoGanhadorLocal (ConcursoId);
    
    PRINT 'Created table dbo.ConcursoGanhadorLocal';
END
ELSE
    BEGIN
        PRINT 'Table dbo.ConcursoGanhadorLocal already exists';
    END
GO

IF OBJECT_ID(N'dbo.LotofacilDetalhe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LotofacilDetalhe
    (
        ConcursoId bigint NOT NULL,
        Acumulado bit NOT NULL,
        IndicadorConcursoEspecial int NOT NULL,
        Observacao nvarchar(300) NULL,
        ValorArrecadado decimal(18,2) NULL,
        CONSTRAINT PK_LotofacilDetalhe PRIMARY KEY (ConcursoId),
        CONSTRAINT FK_LotofacilDetalhe_Concurso
            FOREIGN KEY (ConcursoId) REFERENCES dbo.Concurso(Id)
                ON DELETE CASCADE
    );
    
    PRINT 'Created table dbo.LotofacilDetalhe';
END
ELSE
    BEGIN
        PRINT 'Table dbo.LotofacilDetalhe already exists';
    END
GO

IF OBJECT_ID(N'dbo.MegaSenaDetalhe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MegaSenaDetalhe
    (
        ConcursoId bigint NOT NULL,
        Acumulado bit NOT NULL,
        IndicadorConcursoEspecial int NOT NULL,
        Observacao nvarchar(300) NULL,
        ValorArrecadado decimal(18,2) NULL,
        ValorAcumulado decimal(18,2) NULL,
        CONSTRAINT PK_MegaSenaDetalhe PRIMARY KEY (ConcursoId),
        CONSTRAINT FK_MegaSenaDetalhe_Concurso
            FOREIGN KEY (ConcursoId) REFERENCES dbo.Concurso(Id)
                ON DELETE CASCADE
    );
    
    PRINT 'Created table dbo.MegaSenaDetalhe';
END
ELSE
    BEGIN
        PRINT 'Table dbo.MegaSenaDetalhe already exists';
    END
GO