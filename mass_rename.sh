#!/bin/bash
#find . -iname "*face_finder*" -exec rename _dbg.txt .txt '{}' \;
find . -iname "*face_finder*" -exec rename 's/face_finder/face_finder/' '{}' \;