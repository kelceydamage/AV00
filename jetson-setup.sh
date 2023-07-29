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
sudo chgrp -R gpio /sys/class/pwm/pwmchip*/*
sudo chmod -R g+rw /sys/class/pwm/pwmchip*/*

### Notes: ###
# Xavier NX JP5
# pin 15 == PWM 1 == GPIO12 == GPIO3_PCC.04 == D6
# pin 32 == PWM 4 == GPIO07 == GPIO3_PR.00 == A3 (motor 2)
# pin 33 == PWM 0 == GPIO13 == GPIO3_PN.01 == D14 (motor 1)

# gpiochip1 127 -- BCM 17 -- J41 Pin 11 == D2 (motor 1)
# gpiochip1 112 -- BCM 18 -- J41 Pin 12 == D3 (motor 2)