$image = "alottafagina/w7-nexux:latest"

Write-Host "Building image $image ..." -ForegroundColor Cyan
docker build -t $image .
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }

Write-Host "Pushing image $image ..." -ForegroundColor Cyan
docker push $image
if ($LASTEXITCODE -ne 0) { Write-Host "Push failed." -ForegroundColor Red; exit 1 }

Write-Host "Done. Image published: $image" -ForegroundColor Green
