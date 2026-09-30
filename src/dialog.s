; =============================================================================
; dialog: a typewriter text box (in-game) or full-screen text (story screens)
; Text codes: $00 end, $01 fanfare, $0A newline, $0C page break
; =============================================================================
DS_IDLE  = 0
DS_OPEN  = 1
DS_TYPE  = 2
DS_PAGE  = 3
DS_CLEAR = 4
DS_END   = 5
DS_CLOSE = 6

BOX_TOP  = 20                   ; box covers tile rows 20-27 (attribute rows 5-6)

; X = message id. Caller sets dlg_box (1 = box over the map, 0 = story screen).
dialog_open:
    lda msg_lo,x
    sta dlg_ptr
    lda msg_hi,x
    sta dlg_ptr+1
    lda #0
    sta dlg_line
    sta dlg_col
    sta dlg_step
    lda dlg_box
    beq @story
    lda #BOX_TOP+1
    sta dlg_row0
    lda #DS_OPEN
    sta dlg_state
    rts
@story:
    lda #11
    sta dlg_row0
    lda #DS_TYPE
    sta dlg_state
    rts

; X = message id, A = action to run when the box closes
start_talk:
    sta dlg_action
    lda #1
    sta dlg_box
    jmp dialog_open

dialog_update:
    lda dlg_state
    asl a
    tax
    lda dlg_table,x
    sta ptr1
    lda dlg_table+1,x
    sta ptr1+1
    jmp (ptr1)

dlg_table:
    .word dlg_idle, dlg_do_open, dlg_do_type, dlg_do_page, dlg_do_clear, dlg_do_end, dlg_do_close

dlg_idle:
    rts

dlg_do_open:
    lda dlg_step
    cmp #8
    beq @attr
    clc
    adc #BOX_TOP
    ldx #0
    jsr set_vaddr_rc
    lda #32
    jsr vbuf_header
    lda dlg_step
    beq @top
    cmp #7
    beq @bot
    lda #T_BOX_L
    sta tmp0
    lda #T_BLANK
    sta tmp1
    lda #T_BOX_R
    sta tmp2
    bne @emit
@top:
    lda #T_BOX_TL
    sta tmp0
    lda #T_BOX_T
    sta tmp1
    lda #T_BOX_TR
    sta tmp2
    bne @emit
@bot:
    lda #T_BOX_BL
    sta tmp0
    lda #T_BOX_B
    sta tmp1
    lda #T_BOX_BR
    sta tmp2
@emit:
    lda #0
    jsr vbuf_put
    lda tmp0
    jsr vbuf_put
    ldx #28
    lda tmp1
@mid:
    jsr vbuf_put
    dex
    bne @mid
    lda tmp2
    jsr vbuf_put
    lda #0
    jsr vbuf_put
    inc dlg_step
    rts
@attr:
    SET_VADDR $23E8
    lda #16
    jsr vbuf_header
    ldx #16
    lda #$FF
@a:
    jsr vbuf_put
    dex
    bne @a
    lda #DS_TYPE
    sta dlg_state
    rts

dlg_do_type:
    jsr type_one
    lda dlg_state
    cmp #DS_TYPE
    bne @r
    lda pad
    and #(BTN_A|BTN_B)
    beq @r
    jsr type_one
    lda dlg_state
    cmp #DS_TYPE
    bne @r
    jsr type_one
@r:
    rts

type_one:
    ldy #0
    lda (dlg_ptr),y
    bne @more
    lda #DS_END
    sta dlg_state
    jmp show_arrow
@more:
    inc dlg_ptr
    bne :+
    inc dlg_ptr+1
:
    cmp #$0A
    bne @notnl
    inc dlg_line
    lda #0
    sta dlg_col
    rts
@notnl:
    cmp #$0C
    bne @notpage
    lda #DS_PAGE
    sta dlg_state
    jmp show_arrow
@notpage:
    cmp #$01
    bne @char
    SFX SFX_LEARN
    rts
@char:
    pha
    lda dlg_line
    asl a
    clc
    adc dlg_row0
    pha
    lda dlg_col
    clc
    adc #3
    tax
    pla
    jsr set_vaddr_rc
    lda #1
    jsr vbuf_header
    pla
    jsr vbuf_put
    inc dlg_col
    cmp #' '
    beq @r
    lda dlg_col
    and #1
    beq @r
    SFX SFX_TEXT
@r:
    rts

arrow_vaddr:
    lda dlg_row0
    clc
    adc #5
    ldx #28
    jsr set_vaddr_rc
    lda #1
    jmp vbuf_header

show_arrow:
    jsr arrow_vaddr
    lda #T_ARROW
    jmp vbuf_put

hide_arrow:
    jsr arrow_vaddr
    lda #T_BLANK
    jmp vbuf_put

blink_arrow:
    lda frame
    and #$0F
    bne @r
    lda frame
    and #$10
    beq show_arrow
    bne hide_arrow
@r:
    rts

; returns carry set when A/B was pressed this frame
confirm_pressed:
    lda pad_new
    and #(BTN_A|BTN_B)
    beq @no
    sec
    rts
@no:
    clc
    rts

dlg_do_page:
    jsr confirm_pressed
    bcs @go
    jmp blink_arrow
@go:
    jsr hide_arrow
    lda #0
    sta dlg_step
    lda #DS_CLEAR
    sta dlg_state
    rts

; clears text line dlg_step
clear_text_line:
    lda dlg_step
    asl a
    clc
    adc dlg_row0
    ldx #3
    jsr set_vaddr_rc
    lda #26
    jsr vbuf_header
    ldx #26
    lda #0
@l:
    jsr vbuf_put
    dex
    bne @l
    inc dlg_step
    rts

dlg_do_clear:
    jsr clear_text_line
    lda dlg_step
    cmp #3
    bne @r
    lda #0
    sta dlg_line
    sta dlg_col
    lda #DS_TYPE
    sta dlg_state
@r:
    rts

dlg_do_end:
    jsr confirm_pressed
    bcs @go
    jmp blink_arrow
@go:
    jsr hide_arrow
    lda #0
    sta dlg_step
    lda #DS_CLOSE
    sta dlg_state
    rts

dlg_do_close:
    lda dlg_box
    bne @box
    lda dlg_step
    cmp #3
    bcs @done
    jmp clear_text_line
@done:
    lda #DS_IDLE
    sta dlg_state
    rts
@box:
    lda dlg_step
    cmp #8
    beq @attr
    clc
    adc #BOX_TOP
    jsr vbuf_map_row
    inc dlg_step
    rts
@attr:
    SET_VADDR $23E8
    lda #16
    jsr vbuf_header
    ldx #40
@a:
    lda attr_shadow,x
    jsr vbuf_put
    inx
    cpx #56
    bne @a
    lda #DS_IDLE
    sta dlg_state
    rts

; A = tile row (4..29): re-sends that row of the current map to the vbuf
vbuf_map_row:
    pha
    ldx #0
    jsr set_vaddr_rc
    lda #32
    jsr vbuf_header
    pla
    sec
    sbc #4
    pha
    and #1
    asl a
    sta tmp1
    pla
    lsr a
    asl a
    asl a
    asl a
    asl a
    sta tmp2
    ldx #16
@c:
    ldy tmp2
    lda map,y
    tay
    lda mt_tile,y
    clc
    adc tmp1
    jsr vbuf_put
    clc
    adc #1
    jsr vbuf_put
    inc tmp2
    dex
    bne @c
    rts
