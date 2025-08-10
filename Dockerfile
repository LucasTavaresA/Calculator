FROM debian:latest

# raylib dependencies
RUN apt-get update && apt-get install -y git build-essential libx11-dev \
	libxcursor-dev libxrandr-dev libxinerama-dev libxi-dev \
# dotnet dependencies
	wget libicu-dev

WORKDIR /Calculator

COPY . .

RUN wget https://download.visualstudio.microsoft.com/download/pr/ca6cd525-677e-4d3a-b66c-11348a6f920a/ec395f498f89d0ca4d67d903892af82d/dotnet-sdk-8.0.403-linux-x64.tar.gz
RUN mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-8.0.403-linux-x64.tar.gz -C $HOME/dotnet
ENV DOTNET_ROOT=/root/dotnet
ENV PATH=$PATH:/root/dotnet/

RUN dotnet workload restore
RUN make release-linux

CMD ["sh"]
