# References: https://github.com/SeleniumHQ/docker-selenium/blob/master/NodeChrome/Dockerfile
#             https://askubuntu.com/questions/79280/how-to-install-chrome-browser-properly-via-command-line
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
RUN apt-get update \
    && apt-get install -y gnupg2 \
    && apt-get install libglib2.0-0 libglib2.0-data libnss3 libx11-dev -y \
    && echo deb http://dl.google.com/linux/chrome/deb/ stable main > /etc/apt/sources.list.d/google-sources.list \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && apt-get update \
    && apt-get install google-chrome-stable -y

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-and-publish
WORKDIR /bap
COPY . .
RUN cd TheInternet.SystemTests.Raw \
    && rm -fr bin && rm -fr obj \
    && dotnet build "TheInternet.SystemTests.Raw.csproj" -c Release -o /app/build \
    && dotnet publish "TheInternet.SystemTests.Raw.csproj" -c Release -o /app/publish

FROM base AS package
WORKDIR /app
COPY --from=build-and-publish /app/publish .
ENTRYPOINT ["dotnet", "test", "TheInternet.SystemTests.Raw.dll"]
