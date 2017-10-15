FROM microsoft/dotnet:2.0.0-runtime-stretch-arm32v7

ENV ASPNETCORE_URLS http://+:80

ARG source
WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "Humidor.dll"]
