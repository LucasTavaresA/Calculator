FROM debian:latest

# raylib dependencies
RUN apt-get update && apt-get install -y git build-essential libx11-dev \
	libxcursor-dev libxrandr-dev libxinerama-dev libxi-dev \
# dotnet dependencies
	wget libicu-dev

WORKDIR /Calculator

RUN wget https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.300/dotnet-sdk-10.0.300-linux-x64.tar.gz
RUN mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-10.0.300-linux-x64.tar.gz -C $HOME/dotnet
ENV DOTNET_ROOT=/root/dotnet
ENV PATH=$PATH:/root/dotnet/

COPY . .

RUN dotnet workload restore

RUN make release-linux

CMD ["sh"]
