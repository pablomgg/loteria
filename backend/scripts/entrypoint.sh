#!/bin/bash
set -e

# Sobe o SQL Server em background
/opt/mssql/bin/sqlservr &

echo "Aguardando SQL Server ficar pronto..."
until /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD}" -Q "SELECT 1" > /dev/null 2>&1
do
  sleep 2
done

if [ ! -f /tmp/app-initialized ]; then
  echo "Executando DDL..."
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD}" -d master -i /scripts/DDL.sql

  echo "Executando DML..."
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD}" -d master -i /scripts/DML.sql

  touch /tmp/app-initialized
  echo "Banco inicializado."
fi

# traz o sqlservr pro foreground
wait