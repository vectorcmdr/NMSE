#!/bin/bash
cd /tmp
rm -rf NMSE
git clone --depth 1 https://github.com/vectorcmdr/NMSE.git 2>/dev/null || curl -fsSL https://api.github.com/repos/vectorcmdr/NMSE/zipball/main | tar -xz --strip=1 -C /tmp/NMSE
find /tmp/NMSE -type f | head -80
