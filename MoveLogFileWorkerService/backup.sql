DECLARE @MyFileName varchar(100)
SELECT @MyFileName = (SELECT '\\192.168.0.215\backup\ReelTower_' + REPLACE(REPLACE(convert(varchar(50),GetDate(),121), '.', ''), ':' , '') + '.bak')
BACKUP DATABASE [ReelTower]
TO  DISK = @MyFileName
WITH CHECKSUM;