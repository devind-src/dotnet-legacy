CATATAN:
- Pada query update KEY harus di convert sesuai dengan length pada database jika tidak di convert 
  dampaknya execute query menjadi lama karena default dapper di convert menjadi nvarchar(4000)

  Contoh:
  MSSQL
  WHERE switch_key=convert(varchar(50),@switch_key)

  MSSQL & POSTGREE
  WHERE switch_key=cast(@switch_key AS varchar(50))

- Install service
  sc create "SyncNet Core" binPath="C:\SyncNet\Core\Bin\SyncNetCore.exe"
