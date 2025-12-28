USE EcommerceDB;

-- Vérifiez les contraintes de la table Users
EXEC sp_help 'Users';

-- Si nécessaire, modifiez les colonnes pour permettre NULL temporairement
ALTER TABLE Users ALTER COLUMN Address NVARCHAR(MAX) NULL;
ALTER TABLE Users ALTER COLUMN City NVARCHAR(100) NULL;
ALTER TABLE Users ALTER COLUMN PostalCode NVARCHAR(10) NULL;

-- Ou mettez des valeurs par défaut
ALTER TABLE Users 
ADD CONSTRAINT DF_Users_Address DEFAULT '' FOR Address;

ALTER TABLE Users 
ADD CONSTRAINT DF_Users_City DEFAULT '' FOR City;

ALTER TABLE Users 
ADD CONSTRAINT DF_Users_PostalCode DEFAULT '' FOR PostalCode;