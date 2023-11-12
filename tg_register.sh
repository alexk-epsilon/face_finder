#!/bin/bash
BOT_TOKEN="6183107391:AAFlSpKLMAFtojbi1DtxEDSyRYkNRgFZj_U"
API_URL="https://qhl59dpoh4.execute-api.us-east-1.amazonaws.com/Prod"
curl --location --request POST https://api.telegram.org/bot${BOT_TOKEN}/setWebhook?url=${API_URL}