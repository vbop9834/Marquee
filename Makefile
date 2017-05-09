.PHONY: build test docs

test:
	$(info Running Tests...)
	sh build.sh RunTests

build:
	$(info Building Solution...)
	sh build.sh Build

docs:
	$(info Building Solution...)
	sh build.sh GenerateDocs
