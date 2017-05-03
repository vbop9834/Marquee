.PHONY: build test

test:
	$(info Running Tests...)
	sh build.sh RunTests

build:
	$(info Building Solution...)
	sh build.sh Build
