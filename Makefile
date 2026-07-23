# for update docker-compose and .env
docker-up:
	docker compose up -d
	
# for update Dockerfile and C# file 
docker-up-build:
	docker compose up -d --build

# build docker image
docker-build:
	docker build -t dotnet-ecom-api .

migrations-add:
	dotnet ef migrations add $(name) --project EcommerceAPI.Infrastructure --startup-project EcommerceAPI.Api --output-dir Persistence/Migrations

migrations-update:
	dotnet ef --project EcommerceAPI.Infrastructure --startup-project EcommerceAPI.Api database update
	
gh-run:
	gh workflow run ci.yml

# Test CI/CD
act:
	act --secret-file .secrets