.PHONY: help docker-up docker-down docker-rebuild docker-logs docker-shell-db docker-shell-api \
	docker-clean docker-ps db-connect db-exec api-logs docker-compose-validate

# Default target
help:
	@echo "PulseData Docker Commands"
	@echo "========================="
	@echo ""
	@echo "Development:"
	@echo "  make docker-up          - Start database and pgAdmin"
	@echo "  make docker-up-full     - Start full stack (database, pgAdmin, API)"
	@echo "  make docker-down        - Stop all services"
	@echo "  make docker-ps          - Show running containers"
	@echo "  make docker-restart     - Restart all services"
	@echo ""
	@echo "Logs & Monitoring:"
	@echo "  make docker-logs        - Tail logs from all services"
	@echo "  make db-logs            - Tail PostgreSQL logs"
	@echo "  make api-logs           - Tail API logs"
	@echo ""
	@echo "Access:"
	@echo "  make docker-shell-db    - Open shell in PostgreSQL container"
	@echo "  make docker-shell-api   - Open shell in API container"
	@echo "  make db-connect         - Connect to PostgreSQL with psql"
	@echo "  make db-exec QUERY=''   - Execute SQL query in database"
	@echo ""
	@echo "Rebuild & Clean:"
	@echo "  make docker-rebuild     - Rebuild API image and restart stack"
	@echo "  make docker-clean       - Remove all containers and volumes (CLEAR DATABASE)"
	@echo "  make docker-clean-images - Remove Docker images"
	@echo ""
	@echo "Validation:"
	@echo "  make docker-compose-validate - Validate docker-compose.yml"
	@echo ""

# Basic operations
docker-up:
	docker-compose up -d
	@echo "✓ Services started: PostgreSQL, pgAdmin"
	@echo "  - PostgreSQL: localhost:5432"
	@echo "  - pgAdmin: http://localhost:8080"

docker-up-full:
	docker-compose --profile with-api up -d
	@echo "✓ Full stack started: PostgreSQL, pgAdmin, API"
	@echo "  - PostgreSQL: localhost:5432"
	@echo "  - pgAdmin: http://localhost:8080"
	@echo "  - API: http://localhost:8000"

docker-down:
	docker-compose down
	@echo "✓ All services stopped"

docker-restart:
	docker-compose restart
	@echo "✓ Services restarted"

docker-ps:
	docker-compose ps

docker-compose-validate:
	docker-compose config > /dev/null && echo "✓ docker-compose.yml is valid"

# Logs
docker-logs:
	docker-compose logs -f

db-logs:
	docker-compose logs -f postgres

api-logs:
	docker-compose logs -f api

# Shell access
docker-shell-db:
	docker exec -it pulsedata_db sh

docker-shell-api:
	docker exec -it pulsedata_api sh

# Database operations
db-connect:
	docker exec -it pulsedata_db psql -U pulsedata_user -d pulsedata

db-exec:
	@if [ -z "$(QUERY)" ]; then \
		echo "Usage: make db-exec QUERY=\"your sql query\""; \
		exit 1; \
	fi
	docker exec pulsedata_db psql -U pulsedata_user -d pulsedata -c "$(QUERY)"

# Rebuild
docker-rebuild:
	docker-compose build api
	docker-compose up -d
	@echo "✓ API image rebuilt and services restarted"

# Clean operations
docker-clean:
	docker-compose down -v
	@echo "⚠️  All containers removed and volumes cleaned (database data cleared)"

docker-clean-images:
	docker-compose down -v --remove-orphans
	docker rmi pulsedata-api:latest 2>/dev/null || true
	@echo "✓ Containers, volumes, and images cleaned"

# Additional utilities
health-check:
	@echo "Checking service health..."
	@docker ps --format "table {{.Names}}\t{{.Status}}" | grep pulsedata

backup-db:
	@echo "Backing up PostgreSQL database..."
	@docker exec pulsedata_db pg_dump -U pulsedata_user -d pulsedata > backup_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "✓ Database backed up"

restore-db:
	@if [ -z "$(FILE)" ]; then \
		echo "Usage: make restore-db FILE=backup_20240101_120000.sql"; \
		exit 1; \
	fi
	@echo "Restoring database from $(FILE)..."
	@cat $(FILE) | docker exec -i pulsedata_db psql -U pulsedata_user -d pulsedata
	@echo "✓ Database restored"
