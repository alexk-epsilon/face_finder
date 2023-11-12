#!/bin/bash
#find . -iname "*otyabatka*" -exec rename _dbg.txt .txt '{}' \;
find . -iname "*otyabatka*" -exec rename 's/OtYaBatka/face_finder/' '{}' \;