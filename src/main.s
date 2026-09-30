; =============================================================================
;  SECOND LIFE - A Jobless Rebirth Tale
;  An NES fan game inspired by the "reborn in another world" isekai genre.
;  Build: see Makefile (ca65/ld65 from the cc65 suite)
; =============================================================================

.include "chr_ids.inc"
.include "data.inc"

; ---------------------------------------------------------------- hardware
PPUCTRL   = $2000
PPUMASK   = $2001
PPUSTATUS = $2002
OAMADDR   = $2003
PPUSCROLL = $2005
PPUADDR   = $2006
PPUDATA   = $2007
OAMDMA    = $4014
JOY1      = $4016
APUFRAME  = $4017

oam       = $0200

BTN_A      = $80
BTN_B      = $40
BTN_SELECT = $20
BTN_START  = $10
BTN_UP     = $08
BTN_DOWN   = $04
BTN_LEFT   = $02
BTN_RIGHT  = $01

CTRL_NMI   = %10001000          ; NMI on, sprites from $1000, BG from $0000
MASK_ON    = %00011110          ; show BG + sprites, including left 8 px

; ---------------------------------------------------------------- game constants
DIR_DOWN  = 0
DIR_UP    = 1
DIR_LEFT  = 2
DIR_RIGHT = 3

MODE_TITLE = 0
MODE_STORY = 1
MODE_PLAY  = 2
MODE_FINAL = 3

; story flags
F_TUTOR1  = $01                 ; met the tutor, learned Water Bolt
F_BULLIES = $02                 ; chased off the bullies
F_FRIEND  = $04                 ; befriended Mira
F_EXAM    = $08                 ; learned Fireball, final exam given
F_BOSS    = $10                 ; defeated the wyrm
F_MPTIP   = $20                 ; saw the "mana grows" message

; entity types
ET_NONE   = 0
ET_TUTOR  = 1
ET_FRIEND = 2
ET_MOM    = 3
ET_DAD    = 4
ET_SIGN   = 5
ET_SLIME  = 6
ET_WOLF   = 7
ET_BULLY  = 8
ET_BOSS   = 9
ET_WSHOT  = 10
ET_FSHOT  = 11
ET_EFIRE  = 12
ET_POOF   = 13
ET_HEART  = 14

EF_SOLID  = $01
EF_TALK   = $02
EF_ENEMY  = $04

MAX_ENT   = 12
MPMAX_CAP = 30

; after-dialog actions
ACT_NONE        = 0
ACT_LEARN_WATER = 1
ACT_FRIEND      = 2
ACT_LEARN_FIRE  = 3
ACT_HEAL        = 4
ACT_ENDING      = 5
ACT_BOSS_DOWN   = 6

; ---------------------------------------------------------------- zero page
.segment "ZEROPAGE"
nmi_ready:       .res 1
nmi_count:       .res 1
ppu_mask_shadow: .res 1
pad:             .res 1
pad_prev:        .res 1
pad_new:         .res 1
frame:           .res 1
tmp0:            .res 1
tmp1:            .res 1
tmp2:            .res 1
tmp3:            .res 1
tmp4:            .res 1
tmp5:            .res 1
tmp6:            .res 1
tmp7:            .res 1
ptr0:            .res 2
ptr1:            .res 2
vaddr:           .res 2
vbuf_len:        .res 1
oam_idx:         .res 1
rng:             .res 2
ax0:             .res 1
ax1:             .res 1
ay0:             .res 1
ay1:             .res 1
bx0:             .res 1
bx1:             .res 1
by0:             .res 1
by1:             .res 1
spr_x:           .res 1
spr_y:           .res 1
spr_tile:        .res 1
spr_attr:        .res 1
tmp_nx:          .res 1
tmp_ny:          .res 1
dx_mag:          .res 1
dy_mag:          .res 1
dx_dir:          .res 1
dy_dir:          .res 1
aim_fx:          .res 1
aim_fy:          .res 1
aim_tx:          .res 1
aim_ty:          .res 1
cur_ent:         .res 1
cur_tmp:         .res 1
dlg_ptr:         .res 2
; sound (touched by NMI - never shared with main-thread temps)
mus_ptr:         .res 6
snd_ptr:         .res 2
sfx_ptr:         .res 2
nsfx_ptr:        .res 2
sfx_tptr:        .res 2

; ---------------------------------------------------------------- RAM
.segment "BSS"
vbuf:          .res 192
map:           .res 208
attr_shadow:   .res 64
mode:          .res 1
room:          .res 1
flags:         .res 1
room_changed:  .res 1
paused:        .res 1
story_kind:    .res 1
hud_dirty:     .res 1
pal_level:     .res 1
ent_rot:       .res 1
; player
p_x:           .res 1
p_y:           .res 1
p_dir:         .res 1
p_anim:        .res 1
p_hp:          .res 1
p_hpmax:       .res 1
p_mp:          .res 1
p_mpmax:       .res 1
p_inv:         .res 1
p_kb:          .res 1
p_kbdir:       .res 1
p_cast:        .res 1
p_pose:        .res 1
p_moving:      .res 1
p_speed:       .res 1
p_steps:       .res 1
spell:         .res 1
spells_known:  .res 1
mp_timer:      .res 1
spring_timer:  .res 1
; entities
e_type:        .res MAX_ENT
e_x:           .res MAX_ENT
e_y:           .res MAX_ENT
e_dir:         .res MAX_ENT
e_timer:       .res MAX_ENT
e_hp:          .res MAX_ENT
e_state:       .res MAX_ENT
e_param:       .res MAX_ENT
e_vx:          .res MAX_ENT
e_vy:          .res MAX_ENT
e_flash:       .res MAX_ENT
e_kb:          .res MAX_ENT
e_kbdir:       .res MAX_ENT
; dialog
dlg_state:     .res 1
dlg_line:      .res 1
dlg_col:       .res 1
dlg_row0:      .res 1
dlg_box:       .res 1
dlg_step:      .res 1
dlg_action:    .res 1
; sound
mus_on:        .res 1
mus_cur:       .res 1
mus_start:     .res 6
mus_timer:     .res 3
mus_dur:       .res 3
mus_vol:       .res 3
mus_envt:      .res 3
sfx_len:       .res 1
sfx_cur_prio:  .res 1
sfx_lasthi:    .res 1
nsfx_len:      .res 1
snd_lock:      .res 1
sfx_savex:     .res 1
sfx_savey:     .res 1

; ---------------------------------------------------------------- iNES header
.segment "HEADER"
    .byte "NES", $1A
    .byte 2                     ; 2 x 16 KB PRG
    .byte 1                     ; 1 x 8 KB CHR
    .byte $00                   ; mapper 0, horizontal mirroring
    .byte $00
    .res 8, 0

.segment "CHARS"
    .incbin "game.chr"

.segment "VECTORS"
    .word nmi, reset, irq

; =============================================================================
.segment "CODE"

.macro SET_VADDR addr
    lda #<(addr)
    sta vaddr
    lda #>(addr)
    sta vaddr+1
.endmacro

.macro PRINT_AT row, col, str
    lda #<str
    sta ptr0
    lda #>str
    sta ptr0+1
    lda #row
    ldx #col
    jsr ppu_print_at
.endmacro

.macro SFX id
    lda #id
    jsr play_sfx
.endmacro

; ---------------------------------------------------------------- reset
reset:
    sei
    cld
    ldx #$40
    stx APUFRAME
    ldx #$FF
    txs
    inx
    stx PPUCTRL
    stx PPUMASK
    stx $4010
    bit PPUSTATUS
@vb1:
    bit PPUSTATUS
    bpl @vb1
    txa
@clr:
    sta $0000,x
    sta $0100,x
    sta $0300,x
    sta $0400,x
    sta $0500,x
    sta $0600,x
    sta $0700,x
    inx
    bne @clr
    lda #$FF
@clr2:
    sta oam,x
    inx
    bne @clr2
@vb2:
    bit PPUSTATUS
    bpl @vb2

    lda #$5A
    sta rng
    lda #$C3
    sta rng+1
    jsr sound_init
    lda #CTRL_NMI
    sta PPUCTRL
    jsr title_start

main_loop:
    jsr read_pad
    inc frame
    lda mode
    asl a
    tax
    lda mode_table,x
    sta ptr1
    lda mode_table+1,x
    sta ptr1+1
    jsr jmp_ptr1
    jsr wait_frame
    jmp main_loop

jmp_ptr1:
    jmp (ptr1)

mode_table:
    .word title_update, story_update, play_update, final_update

; ---------------------------------------------------------------- NMI
nmi:
    pha
    txa
    pha
    tya
    pha
    lda nmi_ready
    beq @no_ppu
    lda #0
    sta OAMADDR
    lda #>oam
    sta OAMDMA
    jsr vbuf_flush
    lda ppu_mask_shadow
    sta PPUMASK
    bit PPUSTATUS
    lda #0
    sta PPUSCROLL
    sta PPUSCROLL
    lda #CTRL_NMI
    sta PPUCTRL
    lda #0
    sta nmi_ready
@no_ppu:
    jsr sound_update
    inc nmi_count
    pla
    tay
    pla
    tax
    pla
irq:
    rti

vbuf_flush:
    ldx #0
@next:
    lda vbuf,x
    beq @done
    tay
    lda vbuf+1,x
    sta PPUADDR
    lda vbuf+2,x
    sta PPUADDR
    inx
    inx
    inx
@copy:
    lda vbuf,x
    sta PPUDATA
    inx
    dey
    bne @copy
    beq @next
@done:
    rts

; ---------------------------------------------------------------- frame sync
wait_frame:
    jsr vbuf_end
    lda #1
    sta nmi_ready
@w:
    lda nmi_ready
    bne @w
    lda #0
    sta vbuf_len
    sta vbuf
    rts

; A = number of frames
delay_frames:
    sta tmp7
@l:
    jsr wait_frame
    dec tmp7
    bne @l
    rts

ppu_off:
    lda #0
    sta ppu_mask_shadow
    jsr wait_frame
    lda #0
    sta nmi_ready               ; NMI now only runs sound while we poke VRAM
    rts

; turns rendering back on; also uploads the palette at the current pal_level
ppu_on:
    jsr push_palette
    lda #MASK_ON
    sta ppu_mask_shadow
    jmp wait_frame

; ---------------------------------------------------------------- VRAM buffer
; entries: len, addr_hi, addr_lo, data... ; terminated by len 0
; A = run length, destination in vaddr. Clobbers Y.
vbuf_header:
    ldy vbuf_len
    sta vbuf,y
    iny
    lda vaddr+1
    sta vbuf,y
    iny
    lda vaddr
    sta vbuf,y
    iny
    sty vbuf_len
    rts

; A = byte (preserved). Clobbers Y.
vbuf_put:
    ldy vbuf_len
    sta vbuf,y
    iny
    sty vbuf_len
    rts

vbuf_end:
    ldy vbuf_len
    lda #0
    sta vbuf,y
    rts

; A = row, X = col -> vaddr = $2000 + row*32 + col
set_vaddr_rc:
    sta vaddr
    lda #0
    sta vaddr+1
    asl vaddr
    rol vaddr+1
    asl vaddr
    rol vaddr+1
    asl vaddr
    rol vaddr+1
    asl vaddr
    rol vaddr+1
    asl vaddr
    rol vaddr+1
    txa
    clc
    adc vaddr
    sta vaddr
    lda vaddr+1
    adc #$20
    sta vaddr+1
    rts

; ---------------------------------------------------------------- direct PPU (rendering off)
ppu_set_addr:
    bit PPUSTATUS
    lda vaddr+1
    sta PPUADDR
    lda vaddr
    sta PPUADDR
    rts

; A = row, X = col, ptr0 = zero-terminated string
ppu_print_at:
    jsr set_vaddr_rc
    jsr ppu_set_addr
    ldy #0
@l:
    lda (ptr0),y
    beq @done
    sta PPUDATA
    iny
    bne @l
@done:
    rts

; clears nametable 0; attributes set to palette 3 (UI)
clear_nametable:
    bit PPUSTATUS
    lda #$20
    sta PPUADDR
    lda #$00
    sta PPUADDR
    ldx #3
    lda #0
@page:
    ldy #0
@b:
    sta PPUDATA
    iny
    bne @b
    dex
    bne @page
    ldy #192
@c:
    sta PPUDATA
    dey
    bne @c
    lda #$FF
    ldy #64
@a:
    sta PPUDATA
    dey
    bne @a
    rts

; ---------------------------------------------------------------- palette
palette_data:
    .byte $0F,$1A,$0A,$17, $0F,$07,$27,$37, $0F,$11,$21,$31, $0F,$16,$12,$30
    .byte $0F,$07,$37,$11, $0F,$01,$37,$21, $0F,$08,$37,$2A, $0F,$06,$27,$38

dark_sub:   .byte $40, $30, $20, $10
bright_add: .byte $10, $20, $30, $40

push_palette:
    SET_VADDR $3F00
    lda #32
    jsr vbuf_header
    ldx #0
@l:
    lda palette_data,x
    jsr adjust_color
    jsr vbuf_put
    inx
    cpx #32
    bne @l
    rts

; A = colour -> A adjusted for pal_level (0 black .. 4 normal .. 8 white)
adjust_color:
    ldy pal_level
    cpy #4
    beq @done
    bcs @bright
    sta tmp6
    lda tmp6
    sec
    sbc dark_sub,y
    bcc @black
    rts
@black:
    lda #$0F
    rts
@bright:
    sta tmp6
    and #$0F
    cmp #$0D
    bcc @notblack
    lda #$00
    sta tmp6
@notblack:
    lda tmp6
    clc
    adc bright_add-5,y
    cmp #$40
    bcc @done
    lda #$30
@done:
    rts

fade_out:
    lda #4
    sta pal_level
@l:
    jsr push_palette
    lda #4
    jsr delay_frames
    dec pal_level
    bpl @l
    lda #0
    sta pal_level
    rts

fade_in:
    lda #0
    sta pal_level
@l:
    jsr push_palette
    lda #4
    jsr delay_frames
    inc pal_level
    lda pal_level
    cmp #5
    bne @l
    lda #4
    sta pal_level
    rts

; ---------------------------------------------------------------- input / random
read_pad:
    lda pad
    sta pad_prev
    lda #1
    sta JOY1
    sta pad
    lsr a
    sta JOY1
@l:
    lda JOY1
    lsr a
    rol pad
    bcc @l
    lda pad_prev
    eor #$FF
    and pad
    sta pad_new
    rts

; returns random byte in A; preserves X and Y
rand:
    tya
    pha
    ldy #8
    lda rng
@l:
    asl a
    rol rng+1
    bcc @n
    eor #$39
@n:
    dey
    bne @l
    sta rng
    pla
    tay
    lda rng
    rts

; ---------------------------------------------------------------- sprites
; draws a 16x16 metasprite: spr_x, spr_y, spr_tile (TL of 2x2 block), spr_attr
draw16:
    lda dlg_state
    beq @ok
    lda dlg_box
    beq @ok
    lda spr_y
    cmp #145
    bcc @ok
    rts
@ok:
    ldy oam_idx
    cpy #237
    bcc @room
    rts
@room:
    lda spr_tile
    sta tmp3
    sta tmp4
    inc tmp4
    lda spr_attr
    and #$40
    beq @nf
    inc tmp3
    dec tmp4
@nf:
    lda spr_y
    sec
    sbc #1
    sta tmp0
    clc
    adc #8
    sta tmp1
    lda spr_x
    clc
    adc #8
    sta tmp2
    ; top-left
    lda tmp0
    sta oam,y
    lda tmp3
    sta oam+1,y
    lda spr_attr
    sta oam+2,y
    lda spr_x
    sta oam+3,y
    ; top-right
    lda tmp0
    sta oam+4,y
    lda tmp4
    sta oam+5,y
    lda spr_attr
    sta oam+6,y
    lda tmp2
    sta oam+7,y
    ; bottom-left
    lda tmp1
    sta oam+8,y
    lda tmp3
    clc
    adc #2
    sta oam+9,y
    lda spr_attr
    sta oam+10,y
    lda spr_x
    sta oam+11,y
    ; bottom-right
    lda tmp1
    sta oam+12,y
    lda tmp4
    clc
    adc #2
    sta oam+13,y
    lda spr_attr
    sta oam+14,y
    lda tmp2
    sta oam+15,y
    tya
    clc
    adc #16
    sta oam_idx
    rts

draw8:
    lda dlg_state
    beq @ok
    lda dlg_box
    beq @ok
    lda spr_y
    cmp #153
    bcc @ok
    rts
@ok:
    ldy oam_idx
    cpy #249
    bcc @room
    rts
@room:
    lda spr_y
    sec
    sbc #1
    sta oam,y
    lda spr_tile
    sta oam+1,y
    lda spr_attr
    sta oam+2,y
    lda spr_x
    sta oam+3,y
    tya
    clc
    adc #4
    sta oam_idx
    rts

; 32x32 metasprite made of 4x4 tiles laid out row-major from spr_tile
draw32:
    lda dlg_state
    beq @ok
    lda dlg_box
    beq @ok
    lda spr_y
    cmp #129
    bcc @ok
    rts
@ok:
    ldy oam_idx
    cpy #193
    bcc @room
    rts
@room:
    lda spr_y
    sec
    sbc #1
    sta tmp0
    lda #0
    sta tmp1
@row:
    lda #0
    sta tmp2
@col:
    lda tmp1
    asl a
    asl a
    asl a
    clc
    adc tmp0
    sta oam,y
    lda tmp1
    asl a
    asl a
    clc
    adc tmp2
    clc
    adc spr_tile
    sta oam+1,y
    lda spr_attr
    sta oam+2,y
    lda tmp2
    asl a
    asl a
    asl a
    clc
    adc spr_x
    sta oam+3,y
    iny
    iny
    iny
    iny
    inc tmp2
    lda tmp2
    cmp #4
    bne @col
    inc tmp1
    lda tmp1
    cmp #4
    bne @row
    sty oam_idx
    rts

hide_rest:
    ldy oam_idx
    lda #$FF
@l:
    sta oam,y
    iny
    iny
    iny
    iny
    bne @l
    rts

hide_all_sprites:
    lda #0
    sta oam_idx
    jmp hide_rest

; ---------------------------------------------------------------- collision
; carry set if boxes a and b overlap (inclusive bounds)
overlap:
    lda bx1
    cmp ax0
    bcc @no
    lda ax1
    cmp bx0
    bcc @no
    lda by1
    cmp ay0
    bcc @no
    lda ay1
    cmp by0
    bcc @no
    sec
    rts
@no:
    clc
    rts

; X = pixel x, Y = pixel y -> A = metatile id, or $FF when outside the map
tile_at:
    cpy #32
    bcc @out
    cpy #240
    bcs @out
    tya
    sec
    sbc #32
    and #$F0
    sta tmp3
    txa
    lsr a
    lsr a
    lsr a
    lsr a
    ora tmp3
    tay
    lda map,y
    rts
@out:
    lda #$FF
    rts

; X = pixel x, Y = pixel y -> carry set if solid
pt_solid:
    jsr tile_at
    cmp #$FF
    beq @free
    tay
    lda mt_solid,y
    lsr a
    rts
@free:
    clc
    rts

; is the 16x16 body at (tmp_nx, tmp_ny) clear of solid tiles? carry set if blocked
box_blocked:
    lda tmp_nx
    clc
    adc #3
    tax
    lda tmp_ny
    clc
    adc #8
    tay
    jsr pt_solid
    bcs @done
    lda tmp_nx
    clc
    adc #12
    tax
    lda tmp_ny
    clc
    adc #8
    tay
    jsr pt_solid
    bcs @done
    lda tmp_nx
    clc
    adc #3
    tax
    lda tmp_ny
    clc
    adc #15
    tay
    jsr pt_solid
    bcs @done
    lda tmp_nx
    clc
    adc #12
    tax
    lda tmp_ny
    clc
    adc #15
    tay
    jsr pt_solid
@done:
    rts

; aim from (aim_fx, aim_fy) toward (aim_tx, aim_ty)
; -> dx_mag/dx_dir, dy_mag/dy_dir, A = dominant direction
aim:
    lda aim_tx
    sec
    sbc aim_fx
    bcs @xpos
    eor #$FF
    adc #1
    sta dx_mag
    lda #DIR_LEFT
    sta dx_dir
    jmp @y
@xpos:
    sta dx_mag
    lda #DIR_RIGHT
    sta dx_dir
@y:
    lda aim_ty
    sec
    sbc aim_fy
    bcs @ypos
    eor #$FF
    adc #1
    sta dy_mag
    lda #DIR_UP
    sta dy_dir
    jmp @pick
@ypos:
    sta dy_mag
    lda #DIR_DOWN
    sta dy_dir
@pick:
    lda dx_mag
    cmp dy_mag
    bcc @vert
    lda dx_dir
    rts
@vert:
    lda dy_dir
    rts

dir_dx:   .byte 0, 0, $FF, 1
dir_dy:   .byte 1, $FF, 0, 0
dir_bits: .byte BTN_DOWN, BTN_UP, BTN_LEFT, BTN_RIGHT

; =============================================================================
; title screen
; =============================================================================
title_word1: .byte BIG_S, BIG_E, BIG_C, BIG_O, BIG_N, BIG_D, 0
title_word2: .byte BIG_L, BIG_I, BIG_F, BIG_E, 0
str_subtitle: .byte "A Jobless Rebirth Tale", 0
str_press:    .byte "PRESS START", 0
str_blank11:  .byte "           ", 0
str_controls: .byte "A:TALK  B:MAGIC  SEL:SWAP", 0
str_fan:      .byte "an isekai fan game", 0

title_stars:  ; row, col, tile
    .byte 2, 4, T_STAR_S,  3, 12, T_STAR,  2, 21, T_STAR_S,  4, 27, T_MOON
    .byte 5, 2, T_STAR,  9, 5, T_STAR_S,  11, 27, T_STAR,  15, 3, T_STAR_S
    .byte 16, 24, T_STAR_S,  18, 14, T_STAR,  22, 28, T_STAR_S,  27, 3, T_STAR
    .byte $FF

; ptr0 = list of big-letter tile bases, tmp0 = row, tmp1 = col
ppu_big_word:
    lda #0
    sta tmp2                    ; 0 = top half, 2 = bottom half
@half:
    lda tmp0
    ldx tmp2
    beq :+
    clc
    adc #1
:
    ldx tmp1
    jsr set_vaddr_rc
    jsr ppu_set_addr
    ldy #0
@l:
    lda (ptr0),y
    beq @end
    clc
    adc tmp2
    sta PPUDATA
    clc
    adc #1
    sta PPUDATA
    iny
    bne @l
@end:
    lda tmp2
    bne @done
    lda #2
    sta tmp2
    bne @half
@done:
    rts

title_start:
    jsr ppu_off
    jsr hide_all_sprites
    jsr clear_nametable
    lda #<title_word1
    sta ptr0
    lda #>title_word1
    sta ptr0+1
    lda #6
    sta tmp0
    lda #10
    sta tmp1
    jsr ppu_big_word
    lda #<title_word2
    sta ptr0
    lda #>title_word2
    sta ptr0+1
    lda #9
    sta tmp0
    lda #12
    sta tmp1
    jsr ppu_big_word
    PRINT_AT 13, 5, str_subtitle
    PRINT_AT 20, 10, str_press
    PRINT_AT 23, 3, str_controls
    PRINT_AT 26, 7, str_fan
    ldx #0
@stars:
    lda title_stars,x
    cmp #$FF
    beq @sdone
    stx tmp5
    pha
    lda title_stars+1,x
    tax
    pla
    jsr set_vaddr_rc
    jsr ppu_set_addr
    ldx tmp5
    lda title_stars+2,x
    sta PPUDATA
    inx
    inx
    inx
    bne @stars
@sdone:
    lda #0
    sta pal_level
    jsr ppu_on
    lda #MUS_TITLE
    jsr play_music
    jsr fade_in
    lda #MODE_TITLE
    sta mode
    rts

title_update:
    lda frame
    and #$1F
    bne @input
    lda #20
    ldx #10
    jsr set_vaddr_rc
    lda #11
    jsr vbuf_header
    lda frame
    and #$20
    beq @show
    ldx #0
@blank:
    lda #' '
    jsr vbuf_put
    inx
    cpx #11
    bne @blank
    jmp @input
@show:
    ldx #0
@txt:
    lda str_press,x
    jsr vbuf_put
    inx
    cpx #11
    bne @txt
@input:
    lda pad_new
    and #BTN_START
    beq @done
    SFX SFX_START
    jsr new_game
    jsr fade_out
    lda #0
    sta story_kind
    ldx #MSG_INTRO
    jmp story_begin
@done:
    rts

; =============================================================================
; story screens (intro / game over / ending) - full-screen typewriter text
; =============================================================================
; X = message id
story_begin:
    txa
    pha
    jsr ppu_off
    jsr hide_all_sprites
    jsr clear_nametable
    lda #0
    sta pal_level
    jsr ppu_on
    jsr fade_in
    lda #0
    sta dlg_box
    pla
    tax
    jsr dialog_open
    lda #MODE_STORY
    sta mode
    rts

story_update:
    jsr dialog_update
    lda dlg_state
    beq @finished
    rts
@finished:
    lda story_kind
    beq @intro
    cmp #1
    beq @gameover
    ; ending: flash to white, then the final card
    lda #5
    sta pal_level
@flash:
    jsr push_palette
    lda #3
    jsr delay_frames
    inc pal_level
    lda pal_level
    cmp #9
    bne @flash
    lda #60
    jsr delay_frames
    jmp final_start
@intro:
    jsr fade_out
    jsr enter_room
    jsr fade_in
    lda #MODE_PLAY
    sta mode
    ldx #MSG_MOM_1
    lda #ACT_NONE
    jmp start_talk
@gameover:
    jsr respawn
    jsr fade_out
    jsr enter_room
    jsr fade_in
    lda #MODE_PLAY
    sta mode
    rts

; =============================================================================
; final card
; =============================================================================
str_clear1: .byte "CHAPTER 1: CHILDHOOD", 0
str_clear2: .byte "- CLEAR -", 0
str_clear3: .byte "TO BE CONTINUED...", 0
str_clear4: .byte "THANK YOU FOR PLAYING!", 0

final_start:
    jsr ppu_off
    jsr hide_all_sprites
    jsr clear_nametable
    PRINT_AT 9, 6, str_clear1
    PRINT_AT 11, 11, str_clear2
    PRINT_AT 16, 7, str_clear3
    PRINT_AT 22, 5, str_clear4
    lda #0
    sta pal_level
    jsr ppu_on
    lda #MUS_TITLE
    jsr play_music
    jsr fade_in
    lda #MODE_FINAL
    sta mode
    rts

final_update:
    lda pad_new
    and #BTN_START
    beq @r
    jsr fade_out
    jmp title_start
@r:
    rts

; =============================================================================
; new game / respawn / rooms
; =============================================================================
new_game:
    lda #0
    sta flags
    sta spells_known
    sta spell
    lda #5
    sta p_hp
    sta p_hpmax
    lda #4
    sta p_mp
    sta p_mpmax
respawn:
    lda p_hpmax
    sta p_hp
    lda p_mpmax
    sta p_mp
    lda #4
    sta room
    lda #64
    sta p_x
    lda #112
    sta p_y
    lda #DIR_DOWN
    sta p_dir
    lda #0
    sta p_inv
    sta p_kb
    sta p_cast
    sta p_pose
    sta paused
    sta dlg_state
    rts

; loads 'room' and turns the screen back on
enter_room:
    jsr ppu_off
    ldx room
    lda room_map_lo,x
    sta ptr0
    lda room_map_hi,x
    sta ptr0+1
    ldy #0
@copy:
    lda (ptr0),y
    sta map,y
    iny
    cpy #208
    bne @copy

    ; nametable: 4 HUD rows then 13 rows of metatiles
    bit PPUSTATUS
    lda #$20
    sta PPUADDR
    lda #$00
    sta PPUADDR
    lda #0
    ldx #128
@hud:
    sta PPUDATA
    dex
    bne @hud
    lda #0
    sta tmp0                    ; metatile row
@mrow:
    lda #0
    sta tmp1                    ; 0 = top half, 2 = bottom half
@half:
    lda tmp0
    asl a
    asl a
    asl a
    asl a
    sta tmp2
    ldx #16
@col:
    ldy tmp2
    lda map,y
    tay
    lda mt_tile,y
    clc
    adc tmp1
    sta PPUDATA
    clc
    adc #1
    sta PPUDATA
    inc tmp2
    dex
    bne @col
    lda tmp1
    bne @nextrow
    lda #2
    sta tmp1
    bne @half
@nextrow:
    inc tmp0
    lda tmp0
    cmp #13
    bne @mrow
    jsr build_attrs
    ldx #0
@attr:
    lda attr_shadow,x
    sta PPUDATA
    inx
    cpx #64
    bne @attr
    jsr draw_hud_static

    jsr clear_entities
    jsr spawn_room
    lda #7
    sta hud_dirty
    jsr hud_update

    ; music
    ldx room
    lda room_music,x
    cpx #3
    bne @mus
    lda flags
    and #F_BOSS
    bne @forest
    lda #MUS_BOSS
    bne @mus
@forest:
    lda #MUS_FOREST
@mus:
    jsr play_music

    lda #0
    sta dlg_state
    sta paused
    jsr draw_sprites
    jsr ppu_on

    ; entering the nest for the first time
    lda room
    cmp #3
    bne @r
    lda flags
    and #F_BOSS
    bne @r
    ldx #MSG_BOSS_INTRO
    lda #ACT_NONE
    jmp start_talk
@r:
    rts

; Y = metatile row, A = metatile col -> A = palette of that metatile
pal_at:
    cpy #13
    bcs @zero
    sta tmp3
    tya
    asl a
    asl a
    asl a
    asl a
    ora tmp3
    tay
    lda map,y
    tay
    lda mt_pal,y
    rts
@zero:
    lda #0
    rts

build_attrs:
    ldx #0
    lda #$FF
@hudrow:
    sta attr_shadow,x
    inx
    cpx #8
    bne @hudrow
    lda #0
    sta tmp4                    ; top metatile row of this attribute row
@ay:
    lda #0
    sta tmp5                    ; left metatile col
@ax:
    ldy tmp4
    iny
    lda tmp5
    clc
    adc #1
    jsr pal_at                  ; bottom-right
    sta tmp6
    ldy tmp4
    iny
    lda tmp5
    jsr pal_at                  ; bottom-left
    asl tmp6
    asl tmp6
    ora tmp6
    sta tmp6
    ldy tmp4
    lda tmp5
    clc
    adc #1
    jsr pal_at                  ; top-right
    asl tmp6
    asl tmp6
    ora tmp6
    sta tmp6
    ldy tmp4
    lda tmp5
    jsr pal_at                  ; top-left
    asl tmp6
    asl tmp6
    ora tmp6
    sta attr_shadow,x
    inx
    lda tmp5
    clc
    adc #2
    sta tmp5
    cmp #16
    bne @ax
    lda tmp4
    clc
    adc #2
    sta tmp4
    cpx #64
    bne @ay
    rts

; ---------------------------------------------------------------- HUD
str_hp:    .byte "HP", 0
str_mp:    .byte "MP", 0
str_spell: .byte "SPELL:", 0

draw_hud_static:
    PRINT_AT 1, 1, str_hp
    PRINT_AT 2, 1, str_mp
    PRINT_AT 1, 17, str_spell
    lda #2
    ldx #17
    jsr set_vaddr_rc
    jsr ppu_set_addr
    ldx room
    lda room_name_lo,x
    sta ptr0
    lda room_name_hi,x
    sta ptr0+1
    ldy #0
@name:
    lda (ptr0),y
    sta PPUDATA
    iny
    cpy #12
    bne @name
    rts

spell_names: .byte "-----", "WATER", "FIRE "

hud_update:
    lda hud_dirty
    bne @go
    rts
@go:
    lsr hud_dirty
    bcc @mp
    ; hearts
    SET_VADDR $2024
    lda #8
    jsr vbuf_header
    ldx #0
@heart:
    lda #T_HEART
    cpx p_hp
    bcc @put
    lda #T_HEART_E
    cpx p_hpmax
    bcc @put
    lda #0
@put:
    jsr vbuf_put
    inx
    cpx #8
    bne @heart
@mp:
    lsr hud_dirty
    bcc @spell
    SET_VADDR $2044
    lda #5
    jsr vbuf_header
    lda p_mp
    jsr put_2digits
    lda #'/'
    jsr vbuf_put
    lda p_mpmax
    jsr put_2digits
@spell:
    lsr hud_dirty
    bcc @done
    SET_VADDR $2037
    lda #5
    jsr vbuf_header
    ldx #0
    lda spells_known
    beq @sp
    ldx #5
    lda spell
    beq @sp
    ldx #10
@sp:
    ldy #5
    sty tmp0
@spl:
    lda spell_names,x
    jsr vbuf_put
    inx
    dec tmp0
    bne @spl
@done:
    lda #0
    sta hud_dirty
    rts

; A = 0..99 -> two digit characters into the vbuf
put_2digits:
    ldx #'0'
@tens:
    cmp #10
    bcc @ones
    sbc #10
    inx
    bne @tens
@ones:
    pha
    txa
    jsr vbuf_put
    pla
    clc
    adc #'0'
    jmp vbuf_put

str_paused: .byte " - PAUSED - "

; A = 0 restore room name, 1 show PAUSED
hud_pause_text:
    pha
    lda #2
    ldx #17
    jsr set_vaddr_rc
    lda #12
    jsr vbuf_header
    pla
    beq @name
    ldx #0
@p:
    lda str_paused,x
    jsr vbuf_put
    inx
    cpx #12
    bne @p
    rts
@name:
    ldx room
    lda room_name_lo,x
    sta ptr0
    lda room_name_hi,x
    sta ptr0+1
    ldx #12
    lda #0
    sta tmp0
@n:
    ldy tmp0
    lda (ptr0),y
    jsr vbuf_put
    inc tmp0
    dex
    bne @n
    rts

; =============================================================================
; play mode
; =============================================================================
play_update:
    lda dlg_state
    beq @nodlg
    jsr dialog_update
    lda dlg_state
    bne @draw
    jsr run_dlg_action
    lda mode
    cmp #MODE_PLAY
    beq @draw
    rts
@nodlg:
    lda paused
    beq @running
    lda pad_new
    and #BTN_START
    beq @draw
    lda #0
    sta paused
    jsr hud_pause_text
    jmp @draw
@running:
    lda pad_new
    and #BTN_START
    beq @go
    lda #1
    sta paused
    jsr hud_pause_text
    SFX SFX_TEXT
    jmp @draw
@go:
    lda #0
    sta room_changed
    jsr player_update
    lda mode
    cmp #MODE_PLAY
    beq :+
    rts
:
    lda room_changed
    bne @draw
    lda dlg_state
    bne @draw
    jsr entities_update
    lda p_hp
    bne @draw
    jmp player_died
@draw:
    jsr hud_update
    jmp draw_sprites

player_died:
    SFX SFX_KILL
    lda #0
    sta mus_on
    jsr silence_music
    lda #$FF
    sta mus_cur
    lda #0
    sta p_inv
    jsr draw_sprites
    lda #50
    jsr delay_frames
    jsr fade_out
    lda #1
    sta story_kind
    ldx #MSG_GAMEOVER
    jmp story_begin

draw_sprites:
    lda #0
    sta oam_idx
    jsr draw_player
    ldx ent_rot
    inx
    cpx #MAX_ENT
    bcc :+
    ldx #0
:
    stx ent_rot
    lda #MAX_ENT
    sta tmp5
@l:
    stx tmp6
    jsr draw_entity
    ldx tmp6
    inx
    cpx #MAX_ENT
    bcc :+
    ldx #0
:
    dec tmp5
    bne @l
    jmp hide_rest

; ---------------------------------------------------------------- dialog actions
run_dlg_action:
    lda dlg_action
    ldx #ACT_NONE
    stx dlg_action
    cmp #ACT_LEARN_WATER
    bne @a2
    lda flags
    ora #F_TUTOR1
    sta flags
    lda #1
    sta spells_known
    lda #0
    sta spell
    lda #4
    sta hud_dirty
    rts
@a2:
    cmp #ACT_FRIEND
    bne @a3
    lda flags
    ora #F_FRIEND
    sta flags
    lda p_hpmax
    clc
    adc #2
    sta p_hpmax
    sta p_hp
    lda #1
    sta hud_dirty
    rts
@a3:
    cmp #ACT_LEARN_FIRE
    bne @a4
    lda flags
    ora #F_EXAM
    sta flags
    lda #2
    sta spells_known
    lda #1
    sta spell
    lda #4
    sta hud_dirty
    rts
@a4:
    cmp #ACT_HEAL
    bne @a5
    lda p_hpmax
    sta p_hp
    lda p_mpmax
    sta p_mp
    lda #3
    sta hud_dirty
    rts
@a5:
    cmp #ACT_ENDING
    bne @a6
    jsr fade_out
    lda #2
    sta story_kind
    lda #MUS_TITLE
    jsr play_music
    ldx #MSG_ENDING
    jmp story_begin
@a6:
    cmp #ACT_BOSS_DOWN
    bne @r
    lda #MUS_FOREST
    jmp play_music
@r:
    rts

.include "dialog.s"
.include "player.s"
.include "entities.s"
.include "sound.s"

.segment "RODATA"
METATILE_TABLES
GAME_DATA
