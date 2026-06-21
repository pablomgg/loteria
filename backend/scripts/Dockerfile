FROM mcr.microsoft.com/mssql/server:2022-latest

USER root
WORKDIR /scripts
COPY . /scripts

RUN chmod +x /scripts/entrypoint.sh

USER mssql

ENTRYPOINT ["/bin/bash", "/scripts/entrypoint.sh"]
CMD ["/opt/mssql/bin/sqlservr"]