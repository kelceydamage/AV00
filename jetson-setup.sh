# Install C#
# Download package
cd $HOME
wget https://download.visualstudio.microsoft.com/download/pr/93db1aea-6913-4cdc-8129-23e3e3de8dd1/4a942a2fbbb6ca6667c01ec414096ee0/dotnet-sdk-8.0.100-preview.5.23303.2-linux-arm64.tar.gz
# Make dir and extract
mkdir -p $HOME/dotnet
tar zxf dotnet-sdk-8.0.100-preview.5.23303.2-linux-arm64.tar.gz -C $HOME/dotnet
# Add export to .bashrc
echo "export DOTNET_ROOT=$HOME/dotnet" >> $HOME/.bashrc
echo "export PATH=$PATH:$HOME/dotnet" >> $HOME/.bashrc
# Grant gpio group acces to GPIO in /sys
sudo chgrp -R gpio /sys/class/gpio
sudo chmod -R g+rw /sys/class/gpio
