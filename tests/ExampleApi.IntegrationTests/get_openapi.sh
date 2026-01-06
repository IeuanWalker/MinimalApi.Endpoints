#!/bin/bash
dotnet run --project ../../example/ExampleApi --no-build > /dev/null 2>&1 &
PID=$!
sleep 8
curl -s http://localhost:5000/openapi/v1.json > /tmp/actual_openapi.json
kill $PID 2>/dev/null
wait $PID 2>/dev/null
cat /tmp/actual_openapi.json
