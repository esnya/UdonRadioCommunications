#!/bin/sh

VERSION=$1
ls dist
for src in $(ls dist/*.unitypackage)
do
  dst=$(echo $src | sed -re "s/(-.+?)?\.unitypackage/-v$VERSION.unitypackage/g")
  echo mv dist/$src dist/$dst
done
ls dist
