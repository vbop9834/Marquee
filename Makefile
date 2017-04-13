.PHONY: build docs test

build:
	sh build.sh Build

test:
	sh build.sh Test

docs:
	sh build.sh GenerateDocs
