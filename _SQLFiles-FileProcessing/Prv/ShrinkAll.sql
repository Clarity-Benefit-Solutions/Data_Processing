
GO

use Alegeus_File_Processing;
alter database Alegeus_File_Processing SET RECOVERY SIMPLE;;
go
DBCC SHRINKDATABASE(N'Alegeus_File_Processing' )
GO
use Alegeus_File_Processing;
go
DBCC SHRINKFILE (N'Alegeus_File_Processing' , 0, TRUNCATEONLY)
GO
use Alegeus_File_Processing;
go
DBCC SHRINKFILE (N'Alegeus_File_Processing_Log' , EMPTYFILE)
GO

alter database Alegeus_ErrorLog SET RECOVERY SIMPLE;;
go
USE [Alegeus_ErrorLog]
GO
DBCC SHRINKDATABASE(N'Alegeus_ErrorLog' )
GO

USE [Alegeus_ErrorLog]
GO
DBCC SHRINKFILE (N'Alegeus_ErrorLog' , 0, TRUNCATEONLY)
GO
DBCC SHRINKFILE (N'Alegeus_ErrorLog_Log' , EMPTYFILE)
GO

alter database COBRA SET RECOVERY SIMPLE;;
go

use COBRA;
go

DBCC SHRINKDATABASE(N'COBRA' )
GO
USE [COBRA]
GO
DBCC SHRINKFILE (N'COBRA' , 0, TRUNCATEONLY)
GO
DBCC SHRINKFILE (N'COBRA_Log' ,EMPTYFILE)
GO




alter database COBRA SET RECOVERY FULL ;;
go
alter database Alegeus_ErrorLog SET RECOVERY FULL ;;
go
alter database Alegeus_File_Processing SET RECOVERY FULL ;;
go
