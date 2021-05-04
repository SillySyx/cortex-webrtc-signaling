FROM mcr.microsoft.com/dotnet/sdk:5.0
	WORKDIR /app
	ENV ASPNETCORE_URLS=http://+:80;https://+:443
	EXPOSE 80
	EXPOSE 443

	COPY src/websocket-server.csproj ./
	RUN dotnet restore

	COPY src/* ./

	ENTRYPOINT ["dotnet", "run"]