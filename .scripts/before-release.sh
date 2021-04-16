#!/bin/sh

VERSION=$1
ls dist
for src in $(ls dist/*.unitypackage)
do
  dst=$(echo $src | sed -re "s/(-.+?)?\.unitypackage/-v$VERSION.unitypackage/g")
  mv $src $dst
done
ls dist
