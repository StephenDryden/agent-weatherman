# Weather Agent Makefile
# This makefile provides commands to build, test, and run the Weather Agent

# Variables
IMAGE_NAME := agent-weatherman
IMAGE_TAG := latest
CONTAINER_NAME := weather-agent
SOLUTION_FILE := AgentWeatherman.sln

# Default target
.PHONY: help
help: ## Show this help message
	@echo "Weather Agent - Available commands:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'

# Development commands
.PHONY: restore
restore: ## Restore NuGet packages
	dotnet restore $(SOLUTION_FILE)

.PHONY: build
build: ## Build the solution
	dotnet build $(SOLUTION_FILE) --configuration Release

.PHONY: clean
clean: ## Clean build artifacts
	dotnet clean $(SOLUTION_FILE)
	docker rmi $(IMAGE_NAME):$(IMAGE_TAG) 2>/dev/null || true

.PHONY: run
run: ## Run the application locally
	@echo "Make sure to set your environment variables:"
	@echo "  export WEATHERAGENT_AgentSettings__GitHubToken='your-github-token'"
	@echo "  export WEATHERAGENT_AgentSettings__McpServerUrl='ws://localhost:3000'"
	@echo ""
	dotnet run --project AgentWeatherman/AgentWeatherman.csproj

# Docker commands
.PHONY: docker-build
docker-build: ## Build Docker image
	docker build -t $(IMAGE_NAME):$(IMAGE_TAG) .

.PHONY: docker-run
docker-run: ## Run Docker container (requires environment variables)
	@echo "Starting Weather Agent container..."
	@echo "Make sure you have set the required environment variables:"
	@echo "  GITHUB_TOKEN - Your GitHub personal access token"
	@echo "  MCP_SERVER_URL - URL to your MCP server (default: ws://mcp-server:3000)"
	@echo ""
	docker run -it --rm \
		--name $(CONTAINER_NAME) \
		-e WEATHERAGENT_AgentSettings__GitHubToken="$(GITHUB_TOKEN)" \
		-e WEATHERAGENT_AgentSettings__McpServerUrl="$(MCP_SERVER_URL)" \
		$(IMAGE_NAME):$(IMAGE_TAG)

.PHONY: docker-run-local
docker-run-local: ## Run Docker container connecting to local MCP server
	@echo "Starting Weather Agent container (connecting to local MCP server)..."
	docker run -it --rm \
		--name $(CONTAINER_NAME) \
		--network host \
		-e WEATHERAGENT_AgentSettings__GitHubToken="$(GITHUB_TOKEN)" \
		-e WEATHERAGENT_AgentSettings__McpServerUrl="ws://localhost:3000" \
		$(IMAGE_NAME):$(IMAGE_TAG)

.PHONY: docker-stop
docker-stop: ## Stop running container
	docker stop $(CONTAINER_NAME) 2>/dev/null || true

.PHONY: docker-logs
docker-logs: ## Show container logs
	docker logs $(CONTAINER_NAME)

# Development with Docker Compose (for integration with MCP server)
.PHONY: compose-up
compose-up: ## Start application with Docker Compose
	@echo "Note: This requires a docker-compose.yml file with MCP server configuration"
	docker compose up --build

.PHONY: compose-down
compose-down: ## Stop Docker Compose services
	docker compose down

# AWS ECR commands
.PHONY: ecr-login
ecr-login: ## Login to AWS ECR (requires AWS CLI and proper credentials)
	@echo "Logging into AWS ECR..."
	@echo "Make sure you have set: AWS_ACCOUNT_ID and AWS_REGION"
	aws ecr get-login-password --region $(AWS_REGION) | docker login --username AWS --password-stdin $(AWS_ACCOUNT_ID).dkr.ecr.$(AWS_REGION).amazonaws.com

.PHONY: ecr-create-repo
ecr-create-repo: ## Create ECR repository
	aws ecr create-repository --repository-name $(IMAGE_NAME) --region $(AWS_REGION) || true

.PHONY: ecr-push
ecr-push: ecr-create-repo ## Tag and push image to ECR
	@echo "Tagging and pushing to ECR..."
	docker tag $(IMAGE_NAME):$(IMAGE_TAG) $(AWS_ACCOUNT_ID).dkr.ecr.$(AWS_REGION).amazonaws.com/$(IMAGE_NAME):$(IMAGE_TAG)
	docker push $(AWS_ACCOUNT_ID).dkr.ecr.$(AWS_REGION).amazonaws.com/$(IMAGE_NAME):$(IMAGE_TAG)

# Testing
.PHONY: test
test: ## Run tests (if any exist)
	dotnet test $(SOLUTION_FILE)

# All-in-one commands
.PHONY: all
all: clean restore build docker-build ## Clean, restore, build, and create Docker image

.PHONY: quick-start
quick-start: ## Quick start guide
	@echo "=== Weather Agent Quick Start ==="
	@echo ""
	@echo "1. Set your GitHub token:"
	@echo "   export GITHUB_TOKEN='your-github-personal-access-token'"
	@echo ""
	@echo "2. Make sure your MCP weather server is running on ws://localhost:3000"
	@echo ""
	@echo "3. Choose one of these options:"
	@echo "   - Run locally: make run"
	@echo "   - Run in Docker: make docker-build && make docker-run-local"
	@echo ""
	@echo "For more commands, run: make help"