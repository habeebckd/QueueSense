# ==============================================================================
# QueueSense — Docker Management Makefile
# ==============================================================================

.PHONY: help up down build rebuild restart logs ps db migrate health

.DEFAULT_GOAL := help

help: ## Display available commands
	@echo "QueueSense — Available Management Commands:"
	@echo "  make up       - Start QueueSense containers in detached mode"
	@echo "  make down     - Stop containers without deleting volumes"
	@echo "  make build    - Build all Docker images"
	@echo "  make rebuild  - Rebuild and restart all containers"
	@echo "  make restart  - Restart all running services"
	@echo "  make logs     - View and stream logs from all containers"
	@echo "  make ps       - List container status and port mappings"
	@echo "  make db       - Open PostgreSQL psql interactive shell"
	@echo "  make migrate  - Apply pending EF Core migrations safely"
	@echo "  make health   - Inspect container health status"

up: ## Start QueueSense containers
	docker compose up -d

down: ## Stop containers without deleting volumes
	docker compose down

build: ## Build application images
	docker compose build

rebuild: ## Rebuild and restart containers
	docker compose up -d --build

restart: ## Restart services
	docker compose restart

logs: ## Show container logs
	docker compose logs -f

ps: ## Show container status
	docker compose ps

db: ## Open PostgreSQL terminal
	docker compose exec postgres psql -U postgres -d jobflow

migrate: ## Apply EF Core migrations safely
	docker compose exec backend dotnet ef database update || dotnet ef database update --project src/backend/JobFlow.Infrastructure --startup-project src/backend/JobFlow.Api

health: ## Check container health
	docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Health}}"
