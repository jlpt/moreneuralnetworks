; =============================================================================
; player: movement, room exits, talking, casting
; =============================================================================
spell_cost:  .byte 1, 2
talk_dx:     .byte 0, 0, <-12, 12
talk_dy:     .byte 12, <-14, 0, 0
shot_off_x:  .byte 4, 4, <-4, 12
shot_off_y:  .byte 12, <-2, 6, 6

player_update:
    lda p_inv
    beq :+
    dec p_inv
:
    lda p_cast
    beq :+
    dec p_cast
:
    lda p_pose
    beq :+
    dec p_pose
:
    ; knockback overrides input
    lda p_kb
    beq @input
    dec p_kb
    lda p_kbdir
    jsr player_nudge
    lda p_kbdir
    jmp player_nudge

@input:
    lda #0
    sta p_moving
    lda frame
    and #1
    clc
    adc #1
    sta p_speed

    lda pad
    and #BTN_UP
    beq :+
    jsr move_up
    lda room_changed
    bne @moved
:
    lda pad
    and #BTN_DOWN
    beq :+
    jsr move_down
    lda room_changed
    bne @moved
:
    lda pad
    and #BTN_LEFT
    beq :+
    jsr move_left
    lda room_changed
    bne @moved
:
    lda pad
    and #BTN_RIGHT
    beq :+
    jsr move_right
    lda room_changed
    bne @moved
:
    jsr update_facing
    lda p_moving
    beq @still
    inc p_anim
    jmp @actions
@still:
    lda #0
    sta p_anim
@actions:
    lda pad_new
    and #BTN_A
    beq :+
    jsr try_talk
    lda dlg_state
    bne @moved
:
    lda pad_new
    and #BTN_B
    beq :+
    jsr try_cast
:
    lda pad_new
    and #BTN_SELECT
    beq :+
    jsr swap_spell
:
    jsr regen
@moved:
    rts

; facing follows a newly pressed direction, or any held one if the current isn't held
update_facing:
    ldx #0
@new:
    lda pad_new
    and dir_bits,x
    bne @set
    inx
    cpx #4
    bne @new
    ldx p_dir
    lda pad
    and dir_bits,x
    bne @r
    ldx #0
@held:
    lda pad
    and dir_bits,x
    bne @set
    inx
    cpx #4
    bne @held
@r:
    rts
@set:
    stx p_dir
    rts

; A = dir: move the player 1px if possible (no room exits)
player_nudge:
    tay
    lda p_x
    clc
    adc dir_dx,y
    sta tmp_nx
    lda p_y
    clc
    adc dir_dy,y
    sta tmp_ny
    lda tmp_nx
    cmp #241
    bcs @r
    lda tmp_ny
    cmp #24
    bcc @r
    cmp #225
    bcs @r
    jsr player_blocked
    bcs @r
    lda tmp_nx
    sta p_x
    lda tmp_ny
    sta p_y
@r:
    rts

; is the player body at (tmp_nx, tmp_ny) blocked? carry set if yes
player_blocked:
    jsr box_blocked
    bcs @r
    ; solid NPCs
    lda tmp_nx
    clc
    adc #3
    sta ax0
    adc #9
    sta ax1
    lda tmp_ny
    clc
    adc #8
    sta ay0
    adc #7
    sta ay1
    ldx #0
@l:
    ldy e_type,x
    lda et_flags,y
    and #EF_SOLID
    beq @next
    lda e_x,x
    clc
    adc #1
    sta bx0
    adc #13
    sta bx1
    lda e_y,x
    clc
    adc #4
    sta by0
    adc #11
    sta by1
    jsr overlap
    bcs @r
@next:
    inx
    cpx #MAX_ENT
    bne @l
    clc
@r:
    rts

move_up:
    lda #DIR_UP
    sta tmp5
    lda p_speed
    sta p_steps
@l:
    lda p_y
    cmp #24
    bcs @ok
    jmp exit_north
@ok:
    jsr step_player
    bcs @r
    dec p_steps
    bne @l
@r:
    rts

move_down:
    lda #DIR_DOWN
    sta tmp5
    lda p_speed
    sta p_steps
@l:
    lda p_y
    cmp #225
    bcc @ok
    jmp exit_south
@ok:
    jsr step_player
    bcs @r
    dec p_steps
    bne @l
@r:
    rts

move_left:
    lda #DIR_LEFT
    sta tmp5
    lda p_speed
    sta p_steps
@l:
    lda p_x
    bne @ok
    jmp exit_west
@ok:
    jsr step_player
    bcs @r
    dec p_steps
    bne @l
@r:
    rts

move_right:
    lda #DIR_RIGHT
    sta tmp5
    lda p_speed
    sta p_steps
@l:
    lda p_x
    cmp #240
    bcc @ok
    jmp exit_east
@ok:
    jsr step_player
    bcs @r
    dec p_steps
    bne @l
@r:
    rts

; one pixel in direction tmp5; carry set if blocked
step_player:
    ldy tmp5
    lda p_x
    clc
    adc dir_dx,y
    sta tmp_nx
    lda p_y
    clc
    adc dir_dy,y
    sta tmp_ny
    jsr player_blocked
    bcs @r
    lda tmp_nx
    sta p_x
    lda tmp_ny
    sta p_y
    inc p_moving
    clc
@r:
    rts

exit_west:
    lda room
    and #3
    beq blocked_exit
    dec room
    lda #238
    sta p_x
    jmp change_room
exit_east:
    lda room
    and #3
    cmp #3
    beq blocked_exit
    inc room
    lda #2
    sta p_x
    jmp change_room
exit_north:
    lda room
    cmp #4
    bcc blocked_exit
    sbc #4
    sta room
    lda #222
    sta p_y
    jmp change_room
exit_south:
    lda room
    cmp #4
    bcs blocked_exit
    adc #4
    sta room
    lda #26
    sta p_y
change_room:
    lda #1
    sta room_changed
    jmp enter_room
blocked_exit:
    rts

; ---------------------------------------------------------------- talking
try_talk:
    ldx p_dir
    lda p_x
    clc
    adc #8
    clc
    adc talk_dx,x
    sta ax0
    sta ax1
    lda p_y
    clc
    adc #10
    clc
    adc talk_dy,x
    sta ay0
    sta ay1
    ldx #0
@l:
    ldy e_type,x
    lda et_flags,y
    and #EF_TALK
    beq @next
    ; generous box around the NPC
    lda e_x,x
    sec
    sbc #4
    bcs :+
    lda #0
:
    sta bx0
    lda e_x,x
    clc
    adc #19
    sta bx1
    lda e_y,x
    sec
    sbc #4
    sta by0
    clc
    adc #26
    sta by1
    jsr overlap
    bcs npc_talk
@next:
    inx
    cpx #MAX_ENT
    bne @l
    rts

; X = entity being talked to
npc_talk:
    lda e_type,x
    cmp #ET_TUTOR
    bne :+
    jmp talk_tutor
:
    cmp #ET_FRIEND
    bne :+
    jmp talk_friend
:
    cmp #ET_MOM
    bne :+
    jmp talk_mom
:
    cmp #ET_DAD
    bne :+
    jmp talk_dad
:
    ; sign
    lda e_param,x
    tax
    lda #ACT_NONE
    jmp start_talk

talk_tutor:
    lda flags
    and #F_TUTOR1
    bne @t1
    ldx #MSG_TUTOR_1
    lda #ACT_LEARN_WATER
    jmp start_talk
@t1:
    lda flags
    and #F_BOSS
    beq @t2
    ldx #MSG_TUTOR_GRAD
    lda #ACT_ENDING
    jmp start_talk
@t2:
    lda flags
    and #F_EXAM
    beq @t3
    ldx #MSG_TUTOR_HINT2
    lda #ACT_NONE
    jmp start_talk
@t3:
    lda flags
    and #F_FRIEND
    beq @t4
    ldx #MSG_TUTOR_EXAM
    lda #ACT_LEARN_FIRE
    jmp start_talk
@t4:
    ldx #MSG_TUTOR_HINT1
    lda #ACT_NONE
    jmp start_talk

talk_friend:
    lda flags
    and #F_BULLIES
    bne @f1
    ldx #MSG_FRIEND_SCARED
    lda #ACT_NONE
    jmp start_talk
@f1:
    lda flags
    and #F_FRIEND
    bne @f2
    ldx #MSG_FRIEND_THANKS
    lda #ACT_FRIEND
    jmp start_talk
@f2:
    lda flags
    and #(F_EXAM|F_BOSS)
    cmp #F_EXAM
    bne @f3
    ldx #MSG_FRIEND_WORRY
    lda #ACT_NONE
    jmp start_talk
@f3:
    ldx #MSG_FRIEND_IDLE
    lda #ACT_NONE
    jmp start_talk

talk_mom:
    lda flags
    and #F_TUTOR1
    bne @m1
    ldx #MSG_MOM_1
    lda #ACT_NONE
    jmp start_talk
@m1:
    lda flags
    and #F_BOSS
    beq @m2
    ldx #MSG_MOM_END
    lda #ACT_HEAL
    jmp start_talk
@m2:
    ldx #MSG_MOM_HEAL
    lda #ACT_HEAL
    jmp start_talk

talk_dad:
    lda flags
    and #F_BOSS
    beq @d1
    ldx #MSG_DAD_PROUD
    lda #ACT_NONE
    jmp start_talk
@d1:
    lda flags
    and #F_EXAM
    beq @d2
    ldx #MSG_DAD_GO
    lda #ACT_NONE
    jmp start_talk
@d2:
    lda flags
    and #F_TUTOR1
    beq @d3
    ldx #MSG_DAD_BLOCK
    lda #ACT_NONE
    jmp start_talk
@d3:
    ldx #MSG_DAD_1
    lda #ACT_NONE
    jmp start_talk

; ---------------------------------------------------------------- magic
swap_spell:
    lda spells_known
    cmp #2
    bcc @r
    lda spell
    eor #1
    sta spell
    lda #4
    ora hud_dirty
    sta hud_dirty
    SFX SFX_TEXT
@r:
    rts

try_cast:
    lda spells_known
    beq @r
    lda p_cast
    bne @r
    ldy spell
    lda p_mp
    cmp spell_cost,y
    bcs @enough
    SFX SFX_BLOCKED
@r:
    rts
@enough:
    ; at most 2 player shots on screen
    lda #0
    sta tmp0
    ldx #0
@count:
    lda e_type,x
    cmp #ET_WSHOT
    beq @isshot
    cmp #ET_FSHOT
    bne @nc
@isshot:
    inc tmp0
@nc:
    inx
    cpx #MAX_ENT
    bne @count
    lda tmp0
    cmp #2
    bcs @r
    jsr ent_alloc
    bcs @r
    ldy spell
    lda p_mp
    sec
    sbc spell_cost,y
    sta p_mp
    lda #ET_WSHOT
    cpy #0
    beq :+
    lda #ET_FSHOT
:
    sta e_type,x
    ldy p_dir
    tya
    sta e_dir,x
    lda p_x
    clc
    adc shot_off_x,y
    sta e_x,x
    lda p_y
    clc
    adc shot_off_y,y
    sta e_y,x
    lda #14
    sta p_cast
    lda #10
    sta p_pose
    lda hud_dirty
    ora #2
    sta hud_dirty
    lda spell
    bne @firesnd
    SFX SFX_WATER
    jmp @grow
@firesnd:
    SFX SFX_FIRE
@grow:
    ; running dry makes your mana pool grow
    lda p_mp
    bne @done
    lda p_mpmax
    cmp #MPMAX_CAP
    bcs @done
    inc p_mpmax
    lda flags
    and #F_MPTIP
    bne @quiet
    lda flags
    ora #F_MPTIP
    sta flags
    ldx #MSG_MP_GROW
    lda #ACT_NONE
    jmp start_talk
@quiet:
    SFX SFX_PICKUP
@done:
    rts

; mana regenerates slowly; the healing spring restores HP and MP
regen:
    lda spells_known
    beq @spring
    inc mp_timer
    lda mp_timer
    cmp #45
    bcc @spring
    lda #0
    sta mp_timer
    lda p_mp
    cmp p_mpmax
    bcs @spring
    inc p_mp
    lda hud_dirty
    ora #2
    sta hud_dirty
@spring:
    lda p_x
    clc
    adc #8
    tax
    lda p_y
    clc
    adc #12
    tay
    jsr tile_at
    cmp #MT_SPRING
    bne @r
    inc spring_timer
    lda spring_timer
    and #15
    bne @r
    lda p_mp
    cmp p_mpmax
    bcs :+
    inc p_mp
:
    lda spring_timer
    and #31
    bne @hp
    lda p_hp
    cmp p_hpmax
    bcs @hp
    inc p_hp
    SFX SFX_HEAL
@hp:
    lda #3
    ora hud_dirty
    sta hud_dirty
@r:
    rts

; ---------------------------------------------------------------- damage
; A = damage, X = source entity
hurt_player:
    ldy p_inv
    bne @r
    sta tmp0
    lda p_hp
    sec
    sbc tmp0
    bcs :+
    lda #0
:
    sta p_hp
    lda #60
    sta p_inv
    lda #8
    sta p_kb
    lda e_x,x
    sta aim_fx
    lda e_y,x
    sta aim_fy
    lda e_type,x
    cmp #ET_BOSS
    bne :+
    lda aim_fx
    clc
    adc #8
    sta aim_fx
    lda aim_fy
    clc
    adc #8
    sta aim_fy
:
    lda p_x
    sta aim_tx
    lda p_y
    sta aim_ty
    jsr aim
    sta p_kbdir
    lda hud_dirty
    ora #1
    sta hud_dirty
    SFX SFX_HURT
@r:
    rts

; ---------------------------------------------------------------- drawing
draw_player:
    lda p_inv
    and #2
    beq :+
    rts
:
    lda dlg_state
    beq :+
    lda dlg_box
    beq @hide
:
    lda p_x
    sta spr_x
    lda p_y
    sta spr_y
    lda #0
    sta spr_attr
    lda p_anim
    lsr a
    lsr a
    lsr a
    and #1
    sta tmp0                    ; walk frame
    lda p_dir
    cmp #DIR_DOWN
    bne @up
    lda #SPR_PL_DOWN_A
    ldx tmp0
    beq @set
    lda #SPR_PL_DOWN_B
    bne @set
@up:
    cmp #DIR_UP
    bne @side
    lda #SPR_PL_UP_A
    ldx tmp0
    beq @set
    lda #SPR_PL_UP_B
    bne @set
@side:
    cmp #DIR_LEFT
    bne :+
    lda #$40
    sta spr_attr
:
    lda p_pose
    beq @walk
    lda #SPR_PL_CAST
    bne @set
@walk:
    lda #SPR_PL_SIDE_A
    ldx tmp0
    beq @set
    lda #SPR_PL_SIDE_B
@set:
    sta spr_tile
    jmp draw16
@hide:
    rts
