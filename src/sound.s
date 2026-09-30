; =============================================================================
; sound: 3-channel music (pulse 1 lead, pulse 2 harmony, triangle bass)
;        + sound effects on pulse 2 (overrides harmony) and noise
; Music bytes: $00-$7C note, $7D hold, $7E rest, $80|n set duration, $FF loop
; =============================================================================
mus_peak:  .byte 9, 6
mus_floor: .byte 5, 2

sound_init:
    lda #$0F
    sta $4015
    lda #$08
    sta $4001
    sta $4005
    jsr silence_music
    lda #$30
    sta $400C
    lda #$FF
    sta mus_cur
    lda #0
    sta mus_on
    sta sfx_len
    sta nsfx_len
    rts

silence_music:
    lda #$30
    sta $4000
    sta $4004
    lda #$80
    sta $4008
    rts

; A = song id (no-op if it's already playing)
play_music:
    cmp mus_cur
    bne :+
    rts
:
    sta mus_cur
    tax
    lda #0
    sta mus_on
    lda song_ch0_lo,x
    sta mus_ptr
    sta mus_start
    lda song_ch0_hi,x
    sta mus_ptr+1
    sta mus_start+1
    lda song_ch1_lo,x
    sta mus_ptr+2
    sta mus_start+2
    lda song_ch1_hi,x
    sta mus_ptr+3
    sta mus_start+3
    lda song_ch2_lo,x
    sta mus_ptr+4
    sta mus_start+4
    lda song_ch2_hi,x
    sta mus_ptr+5
    sta mus_start+5
    lda #1
    sta mus_timer
    sta mus_timer+1
    sta mus_timer+2
    lda #0
    sta mus_vol
    sta mus_vol+1
    jsr silence_music
    lda #1
    sta mus_on
    rts

; called from NMI
sound_update:
    lda mus_on
    beq @sfx
    ldx #0
    jsr mus_channel
    ldx #1
    jsr mus_channel
    ldx #2
    jsr mus_channel
@sfx:
    lda snd_lock
    bne @r
    jsr sfx_pulse_update
    jsr sfx_noise_update
@r:
    rts

; X = channel
mus_channel:
    dec mus_timer,x
    beq @read
    jmp mus_env
@read:
    txa
    asl a
    tay
    lda mus_ptr,y
    sta snd_ptr
    lda mus_ptr+1,y
    sta snd_ptr+1
@next:
    ldy #0
    lda (snd_ptr),y
    inc snd_ptr
    bne :+
    inc snd_ptr+1
:
    cmp #$FF
    bne @notloop
    txa
    asl a
    tay
    lda mus_start,y
    sta snd_ptr
    lda mus_start+1,y
    sta snd_ptr+1
    jmp @next
@notloop:
    cmp #$80
    bcc @event
    and #$7F
    sta mus_dur,x
    jmp @next
@event:
    pha
    lda mus_dur,x
    sta mus_timer,x
    txa
    asl a
    tay
    lda snd_ptr
    sta mus_ptr,y
    lda snd_ptr+1
    sta mus_ptr+1,y
    pla
    cmp #$7D
    bne :+
    jmp mus_env                 ; hold: keep sounding
:
    cmp #$7E
    beq @rest
    tay
    cpx #2
    beq @tri
    lda mus_peak,x
    sta mus_vol,x
    lda #0
    sta mus_envt,x
    cpx #1
    beq @p2
    lda period_lo,y
    sta $4002
    lda period_hi,y
    sta $4003
    jmp mus_env_write
@p2:
    lda sfx_len
    bne @r
    lda period_lo,y
    sta $4006
    lda period_hi,y
    sta $4007
    jmp mus_env_write
@tri:
    lda #$FF
    sta $4008
    lda period_lo,y
    sta $400A
    lda period_hi,y
    sta $400B
@r:
    rts
@rest:
    cpx #2
    beq @trirest
    lda #0
    sta mus_vol,x
    jmp mus_env_write
@trirest:
    lda #$80
    sta $4008
    rts

; simple decay envelope for the pulse channels
mus_env:
    cpx #2
    bne :+
    rts
:
    inc mus_envt,x
    lda mus_envt,x
    and #3
    bne mus_env_write
    lda mus_vol,x
    cmp mus_floor,x
    beq mus_env_write
    bcc mus_env_write
    dec mus_vol,x
mus_env_write:
    cpx #1
    beq @p2
    lda mus_vol,x
    ora #$B0
    sta $4000
    rts
@p2:
    lda sfx_len
    bne @r
    lda mus_vol,x
    ora #$70
    sta $4004
@r:
    rts

; A = sfx id; preserves X and Y
play_sfx:
    stx sfx_savex
    sty sfx_savey
    tax
    lda #1
    sta snd_lock
    lda sfx_lo,x
    sta sfx_tptr
    lda sfx_hi,x
    sta sfx_tptr+1
    ldy #0
    lda (sfx_tptr),y
    bne @noise
    lda sfx_len
    beq @take
    lda sfx_prio,x
    cmp sfx_cur_prio
    bcc @done
@take:
    lda sfx_prio,x
    sta sfx_cur_prio
    lda #$FF
    sta sfx_lasthi
    lda sfx_tptr
    clc
    adc #2
    sta sfx_ptr
    lda sfx_tptr+1
    adc #0
    sta sfx_ptr+1
    ldy #1
    lda (sfx_tptr),y
    sta sfx_len
    jmp @done
@noise:
    lda sfx_tptr
    clc
    adc #2
    sta nsfx_ptr
    lda sfx_tptr+1
    adc #0
    sta nsfx_ptr+1
    ldy #1
    lda (sfx_tptr),y
    sta nsfx_len
@done:
    lda #0
    sta snd_lock
    ldx sfx_savex
    ldy sfx_savey
    rts

sfx_pulse_update:
    lda sfx_len
    beq @r
    ldy #0
    lda (sfx_ptr),y
    sta $4004
    iny
    lda (sfx_ptr),y
    sta $4006
    iny
    lda (sfx_ptr),y
    cmp sfx_lasthi
    beq :+
    sta sfx_lasthi
    sta $4007
:
    lda sfx_ptr
    clc
    adc #3
    sta sfx_ptr
    bcc :+
    inc sfx_ptr+1
:
    dec sfx_len
    bne @r
    lda #$30
    sta $4004
@r:
    rts

sfx_noise_update:
    lda nsfx_len
    beq @r
    ldy #0
    lda (nsfx_ptr),y
    sta $400C
    iny
    lda (nsfx_ptr),y
    sta $400E
    lda #$08
    sta $400F
    lda nsfx_ptr
    clc
    adc #2
    sta nsfx_ptr
    bcc :+
    inc nsfx_ptr+1
:
    dec nsfx_len
    bne @r
    lda #$30
    sta $400C
@r:
    rts
