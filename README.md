# Microservice Demo Project

A complete, production-grade microservices demo showcasing the latest technologies in 2025, including ASP.NET Core 8, Dapr, gRPC, OpenTelemetry, RabbitMQ, Redis, and HybridCache.

## 🏗️ Architecture Overview

### Microservices
- **User Service** (Port 8080): Manages user accounts and JWT authentication
- **Product Service** (Port 8081): Manages product catalog using gRPC + HybridCache
- **Order Service** (Port 8082): 	Handles order processing using Dapr Workflow
- **Notification Service** (Port 8083): Sends notifications asynchronously via RabbitMQ

### Infrastructure
- **Redis** (Port 6379): Cache and State Store
- **RabbitMQ** (Port 5672, 15672): Message Queue
- **Prometheus** (Port 9090): Metrics Collection
- **Grafana** (Port 3000): Monitoring Dashboard
- **Dapr**: Service-to-service communication, pub/sub, state management

## 🚀 Technology Stack

### Core Technologies
- **.NET 8**: Main Framework 
- **ASP.NET Core WebAPI**: REST API
- **Entity Framework Core**: ORM with InMemory Database
- **AutoMapper**: Object mapping

### Modern Technologies (2025)
- **Dapr v1.13+**: Distributed application runtime
- **gRPC**: High-performance RPC
- **HybridCache (.NET 8)**: In-memory + distributed cache
- **OpenTelemetry**: Observability (traces, metrics, logs)
- **JWT Authentication**: Manual token-based auth

### Communication
- **REST APIs**: Synchronous communication
- **gRPC**: High-performance inter-service calls
- **RabbitMQ + Dapr Pub/Sub**: Asynchronous messaging

### Observability
- **OpenTelemetry**: Distributed tracing
- **Prometheus**: Metrics collection
- **Grafana**: Visualization and dashboards

## 📋 Prerequisites

- **.NET 8 SDK**
- **Docker & Docker Compose**
- **Dapr CLI**
- **Visual Studio 2022** hoặc **VS Code**

## 🛠️ Setup & Run

### 1. Clone Repository
```bash
git clone <repository-url>
cd MicroserviceDemo
```

### 2. Install Dapr
```bash
# Install Dapr CLI
curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh | /bin/bash

# Initialize Dapr
dapr init
```

### 3. Run with Docker Compose
```bash
# Build and run all services
docker-compose up --build

# or run in background
docker-compose up -d --build
```

### 4. Run Local Development (Alternative)
```bash
# Terminal 1: Infrastructure
docker-compose up redis rabbitmq prometheus grafana

# Terminal 2: User Service
cd Services/UserService
dapr run --app-id user-service --app-port 8080 --dapr-http-port 3500 --components-path ../../Infrastructure/dapr/components -- dotnet run

# Terminal 3: Product Service
cd Services/ProductService
dapr run --app-id product-service --app-port 8081 --dapr-http-port 3501 --components-path ../../Infrastructure/dapr/components -- dotnet run
```

## 🧪 Testing and Demo

### 1. Health Checks
```bash
# User Service
curl http://localhost:8080/api/users/health

# Product Service
curl http://localhost:8081/api/products/health
```

### 2. Authentication
```bash
# Register user
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@demo.com",
    "password": "password123",
    "fullName": "Test User"
  }'

# Login
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

### 3. Product Management
```bash
# Get products (with JWT token)
curl -X GET http://localhost:8081/api/products \
  -H "Authorization: Bearer <your-jwt-token>"

# Create product
curl -X POST http://localhost:8081/api/products \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Demo Product",
    "description": "Product for demo",
    "price": 99.99,
    "stock": 100,
    "category": "Electronics"
  }'
```

## 📊 Monitoring and Observability

### Swagger/OpenAPI
- **User Service**: http://localhost:8080
- **Product Service**: http://localhost:8081

### Monitoring Dashboards
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### Metrics Endpoints
- **User Service**: http://localhost:8080/metrics
- **Product Service**: http://localhost:8081/metrics

## 🏃‍♂️ Demo Scenarios

### Scenario 1: User Authentication Flow
1. Register new user via User Service
2. Login and get JWT token
3. Use token to access protected endpoints
4. View traces in Grafana

### Scenario 2: Product Management with Cache
1. Create products via REST API
2. Get products (observe cache behavior)
3. Call gRPC endpoints for high-performance operations
4. Monitor cache hit/miss in Redis

### Scenario 3: Order Processing with Messaging
1. Create order via Order Service
2. Observe RabbitMQ message flow
3. Check notification delivery
4. Monitor Dapr pub/sub traces

### Scenario 4: Observability and Monitoring
1. Generate traffic across services
2. View distributed traces in Grafana
3. Monitor metrics in Prometheus
4. Analyze performance bottlenecks

## 🧪 Automated Testing

### PowerShell Test Script
```powershell
# Run comprehensive demo tests
.\Scripts\test-demo.ps1
```

This script tests:
- Service health checks
- Authentication flow
- Product operations
- Cache performance
- Order creation (if available)
- End-to-end functionality

## 📈 Current Implementation Status

### ✅ Completed Services
- **User Service**: Full implementation with JWT auth, InMemory DB, HybridCache, OpenTelemetry
- **Product Service**: Complete with gRPC, REST APIs, HybridCache, Dapr integration, stock management

### 🔄 In Progress
- **Order Service**: Dapr Workflow implementation (partial)
- **Notification Service**: Event-driven notifications (planned)

### ✅ Infrastructure
- Complete Docker Compose setup
- Dapr components configuration
- Monitoring stack (Prometheus + Grafana)
- All necessary infrastructure services

## 🔧 Configuration

### JWT Settings
```json
{
  "Jwt": {
    "Secret": "your-secret-key-here-must-be-at-least-32-characters-long",
    "ExpiryHours": 24
  }
}
```

### Cache Settings
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Dapr Components
- **Pub/Sub**: RabbitMQ (Infrastructure/dapr/components/pubsub.yaml)
- **State Store**: Redis (Infrastructure/dapr/components/statestore.yaml)
- **Secrets**: Local file (Infrastructure/dapr/components/secrets.yaml)

## 📚 Learning Points

### Microservices Architecture
- Service separation and boundaries
- Inter-service communication patterns
- Data consistency strategies

### Modern .NET Features
- HybridCache for optimal performance
- Minimal APIs and top-level programs
- Built-in observability

### Dapr Benefits
- Service discovery and invocation
- Pub/sub messaging abstraction
- State management across services
- Secrets management

### Observability Best Practices
- Distributed tracing with OpenTelemetry
- Metrics collection and visualization
- Structured logging and correlation

## 🚨 Troubleshooting

### Common Issues
1. **Port conflicts**: Ensure ports 8080-8083, 3000, 5672, 6379, 9090 are available
2. **Dapr not running**: Run `dapr init` and ensure Dapr sidecar is healthy
3. **Docker issues**: Check Docker Desktop is running and has sufficient resources
4. **JWT issues**: Verify JWT secret is at least 32 characters

### Logs
```bash
# View service logs
docker-compose logs user-service
docker-compose logs product-service

# View Dapr logs
docker-compose logs user-service-dapr
docker-compose logs product-service-dapr
```

## 🎯 Next Steps

1. **Complete remaining services** (Order, Notification)
2. **Add integration tests**
3. **Implement CI/CD pipeline**
4. **Add API Gateway** (YARP or Ocelot)
5. **Deploy to cloud** (Azure Container Apps, AWS Fargate)

## 📖 Documentation

- [Dapr Documentation](https://docs.dapr.io/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [gRPC in .NET](https://docs.microsoft.com/en-us/aspnet/core/grpc/)

---

**This demo demonstrates the application of the most modern microservice technologies in practice, suitable for learning and developing production-ready applications.** 