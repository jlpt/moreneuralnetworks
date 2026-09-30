# Second Life - NES fan game
# Needs: python3, cc65 (ca65 + ld65)

ROM   := second_life.nes
SRC   := $(wildcard src/*.s)

all: $(ROM)

build/game.chr build/chr_ids.inc: tools/make_chr.py
	python3 tools/make_chr.py

build/data.inc: tools/make_data.py
	python3 tools/make_data.py

build/main.o: $(SRC) build/game.chr build/chr_ids.inc build/data.inc
	ca65 -I src -I build --bin-include-dir build -g -o $@ src/main.s

$(ROM): build/main.o src/nrom.cfg
	ld65 -C src/nrom.cfg -o $@ --dbgfile build/game.dbg -m build/game.map build/main.o

clean:
	rm -rf build $(ROM)

.PHONY: all clean

# Scripted full playthrough in a headless emulator (needs node; run `npm install` in tests/ once)
test: $(ROM)
	python3 tools/syms.py build/game.dbg build/syms.json
	node tests/run.mjs $(ROM) tests/playthrough.json build/shots build/syms.json

.PHONY: test
