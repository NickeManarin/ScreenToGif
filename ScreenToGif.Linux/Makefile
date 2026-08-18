DOTNET ?= dotnet
CONFIGURATION ?= Debug

SOLUTION := ../ScreenToGif.Linux.sln
PROJECT := ScreenToGif.Linux.csproj
TEST_PROJECT := ../ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj
PUBLISH_DIR := ../artifacts/linux/$(CONFIGURATION)
APP := $(CURDIR)/$(PUBLISH_DIR)/ScreenToGif.Linux

.PHONY: all restore build test run editor publish install-desktop clean

all: build

restore:
	$(DOTNET) restore $(SOLUTION)

build: restore
	$(DOTNET) build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore

test: restore
	$(DOTNET) test $(TEST_PROJECT) --configuration $(CONFIGURATION) --no-restore

run: build
	$(DOTNET) run --project $(PROJECT) --configuration $(CONFIGURATION) --no-build

editor: build
	$(DOTNET) run --project $(PROJECT) --configuration $(CONFIGURATION) --no-build -- --editor

publish: restore
	$(DOTNET) publish $(PROJECT) --configuration $(CONFIGURATION) --no-restore --output $(PUBLISH_DIR)

install-desktop: publish
	./scripts/install-desktop-entry.sh $(APP)

clean:
	$(DOTNET) clean $(SOLUTION) --configuration $(CONFIGURATION)
