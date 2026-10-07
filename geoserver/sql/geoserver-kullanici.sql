-- GeoServer'in SQL Server kullanicisi: SADECE gs ve gs_onayli semalarindaki view'lari okur.
-- SSMS'te Windows kimligiyle (yonetici olarak) calistirilir. Tekrar calistirmak zararsizdir.
--
-- Neden: GeoServer'in tuttugu parola ele gecerse zarar "haritadaki veriyi okumak"la sinirli kalsin.
-- Semalar dbo'ya ait; view'lar dbo tablolarini sahiplik zinciriyle okur, tablolara ayrica izin gerekmez.

USE WebMapDb;
GO

-- 1) Kullanici yoksa olustur (login + veritabani kullanicisi).
--    Parolayi buraya yazip calistir, sonra dosyaya KAYDETME.
IF SUSER_ID('WebAppGeoSrvr') IS NULL
    CREATE LOGIN WebAppGeoSrvr WITH PASSWORD = N'<GUCLU_PAROLA>', CHECK_POLICY = ON;
IF USER_ID('WebAppGeoSrvr') IS NULL
    CREATE USER WebAppGeoSrvr FOR LOGIN WebAppGeoSrvr;
GO

-- 2) Butun rol uyeliklerinden cikar (db_owner, db_datawriter, db_ddladmin ...).
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER ROLE ' + QUOTENAME(r.name) + N' DROP MEMBER WebAppGeoSrvr;'
FROM sys.database_role_members m
JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
WHERE m.member_principal_id = USER_ID('WebAppGeoSrvr');
EXEC (@sql);
GO

-- 3) Denemede verilen tablo izinlerini geri al.
REVOKE SELECT ON dbo.NetworkElements FROM WebAppGeoSrvr;
REVOKE SELECT ON dbo.Projeler FROM WebAppGeoSrvr;
REVOKE SELECT ON dbo.Kabinler FROM WebAppGeoSrvr;
REVOKE SELECT ON dbo.Menholler FROM WebAppGeoSrvr;
GO

-- 4) Tek izin: iki semayi okumak.
GRANT SELECT ON SCHEMA::gs TO WebAppGeoSrvr;
GRANT SELECT ON SCHEMA::gs_onayli TO WebAppGeoSrvr;
GO

-- 5) Kontrol: sadece CONNECT + gs ve gs_onayli icin SELECT gorunmeli, rol listesi bos olmali.
SELECT pr.permission_name, pr.state_desc, pr.class_desc,
       CASE pr.class_desc
           WHEN 'SCHEMA' THEN SCHEMA_NAME(pr.major_id)
           WHEN 'OBJECT_OR_COLUMN' THEN OBJECT_SCHEMA_NAME(pr.major_id) + '.' + OBJECT_NAME(pr.major_id)
       END AS nesne
FROM sys.database_permissions pr
WHERE pr.grantee_principal_id = USER_ID('WebAppGeoSrvr');

SELECT r.name AS rol
FROM sys.database_role_members m
JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
WHERE m.member_principal_id = USER_ID('WebAppGeoSrvr');
