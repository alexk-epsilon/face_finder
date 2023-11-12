#!/bin/bash
BOT_TOKEN="6183107391:AAFlSpKLMAFtojbi1DtxEDSyRYkNRgFZj_U"
API_URL="https://2xl5gukqth.execute-api.us-east-1.amazonaws.com/Prod"
curl --location --request POST $API_URL -d "/api/v1/Recognition"