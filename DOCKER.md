# Docker Setup Guide for PulseData

This guide explains how to run PulseData services using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (or Docker + Docker Compose)
- At least 2GB RAM available for containers

## Quick Start

### 1. Run Database Only (Recommended for Development)

To start just the PostgreSQL database and pgAdmin:

```bash
docker-compose up -d
```

This will start:

- **PostgreSQL**: Database server on `localhost:5432`
- **pgAdmin**: Web UI for database management on `http://localhost:8080`

### 2. Run Full Stack (Database + API)

To include the API service:

```bash
docker-compose --profile with-api up -d
```

This will additionally start:

- **PulseData API**: REST API on `http://localhost:8000`

### 3. Stop Services

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v
```

## Service Details

### PostgreSQL Database

- **Container**: `pulsedata_db`
- **Host**: `localhost` (from host machine) or `postgres` (from other containers)
- **Port**: `5432`
- **Database**: `pulsedata`
- **Username**: `pulsedata_user`
- **Password**: `pulsedata_pass` (default, change in `.env`)
- **Initialization**: Automatic schema creation and seeding on first run

**Health Check**: Auto-restarts if unhealthy

### pgAdmin

- **Container**: `pulsedata_pgadmin`
- **URL**: `http://localhost:8080`
- **Email**: `admin@pulsedata.local`
- **Password**: `admin`

#### Setup PostgreSQL Connection in pgAdmin:

1. Open `http://localhost:8080`
2. Login with credentials above
3. Right-click "Servers" → "Create" → "Server"
4. **General Tab**:
   - Name: `PulseData`
5. **Connection Tab**:
   - Host name/address: `postgres`
   - Port: `5432`
   - Maintenance database: `pulsedata`
   - Username: `pulsedata_user`
   - Password: `pulsedata_pass`
   - Save password: ✓

### PulseData API

- **Container**: `pulsedata_api`
- **URL**: `http://localhost:8000`
- **API Documentation**: `http://localhost:8000/swagger/index.html` (if Swagger is enabled)
- **Health Check**: `http://localhost:8000/api/health/live`

**Note**: Requires database to be running and healthy before starting.

## Configuration

### Environment Variables

Copy `.env.example` to `.env` and customize:

```bash
cp .env.example .env
```

Key variables:

- `POSTGRES_PASSWORD`: Database password
- `PGADMIN_DEFAULT_PASSWORD`: pgAdmin admin password
- `ASPNETCORE_ENVIRONMENT`: API environment (Development/Production)
- `ConnectionStrings__DefaultConnection`: API database connection string

### Using Environment File with Docker Compose

To use custom `.env` file:

```bash
docker-compose --env-file .env up -d
```

Or simply place `.env` in the same directory as `docker-compose.yml` (automatic).

## Common Tasks

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f postgres
docker-compose logs -f api

# Last 100 lines
docker-compose logs --tail=100
```

### Connect to Database from Host

Using `psql`:

```bash
psql -h localhost -U pulsedata_user -d pulsedata
```

### Execute SQL in Running Container

```bash
docker exec pulsedata_db psql -U pulsedata_user -d pulsedata -c "SELECT * FROM customers LIMIT 5;"
```

### Rebuild Images

```bash
# Rebuild API image
docker-compose build api

# Rebuild and restart
docker-compose up -d --build
```

### Remove All Data and Start Fresh

```bash
docker-compose down -v
docker volume rm pulsedata_postgres_data 2>/dev/null || true
docker-compose up -d
```

### Access Container Shell

```bash
# PostgreSQL container
docker exec -it pulsedata_db sh

# pgAdmin container
docker exec -it pulsedata_pgadmin sh

# API container (if running)
docker exec -it pulsedata_api sh
```

## Development Workflow

### Option 1: Run Database in Docker, API Locally

```bash
# Start services
docker-compose up

# In another terminal, run API locally
cd src/PulseData.API
dotnet run
```

Connection string for local API:

```
Server=localhost;Database=pulsedata;User Id=pulsedata_user;Password=pulsedata_pass;Port=5432;
```

### Option 2: Full Docker Stack

```bash
docker-compose --profile with-api up -d

# View logs
docker-compose logs -f api
```

## Troubleshooting

### Problem: "Connection refused" when connecting to API

**Solution**: Ensure API container is running:

```bash
docker-compose ps
docker-compose logs api
```

### Problem: Database won't start (permission error)

**Solution**: Check volume permissions:

```bash
docker volume ls
docker volume inspect pulsedata_postgres_data
```

### Problem: Port already in use

**Solution**: Change ports in `docker-compose.yml`:

```yaml
ports:
  - '5433:5432' # PostgreSQL on 5433
  - '8081:80' # pgAdmin on 8081
  - '8001:8000' # API on 8001
```

### Problem: Out of disk space

**Solution**: Clean up Docker resources:

```bash
docker system prune -a -v
```

## Docker Compose Profiles

This setup uses profiles to make services optional:

- **No profile** (default): PostgreSQL + pgAdmin
- **with-api**: All services including API

Examples:

```bash
# Start default services only
docker-compose up

# Start with API
docker-compose --profile with-api up -d

# Stop only API but keep database
docker-compose --profile with-api down

# Activate multiple profiles
docker-compose --profile profile1 --profile profile2 up -d
```

## Production Considerations

For production deployments:

1. **Security**:
   - Change all default passwords in `.env`
   - Use environment-specific secrets management
   - Enable https/tls for API

2. **Resources**:
   - Set memory limits in `docker-compose.yml`
   - Configure CPU limits
   - Use volume backups

3. **Monitoring**:
   - Monitor container health
   - Set up centralized logging
   - Implement metrics collection

4. **Database**:
   - Enable regular automated backups
   - Configure replication for high availability
   - Implement connection pooling

5. **API**:
   - Set `ASPNETCORE_ENVIRONMENT=Production`
   - Disable Swagger in production
   - Enable proper error handling

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [pgAdmin Docker Image](https://hub.docker.com/r/dpage/pgadmin4)
- [.NET on Docker](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/container-docker-introduction/)
