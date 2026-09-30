; =============================================================================
; entities: NPCs, enemies, projectiles, effects
; =============================================================================
;               none tutor friend mom dad sign slime wolf bully boss wshot fshot efire poof heart
et_flags:  .byte 0,   3,    3,     3,  3,  3,   4,    4,   4,    4,   0,    0,    0,    0,   0
et_hp:     .byte 0,   0,    0,     0,  0,  0,   2,    3,   1,    20,  0,    0,    0,    0,   0
et_dmg:    .byte 0,   0,    0,     0,  0,  0,   1,    1,   1,    2,   0,    0,    1,    0,   0
et_size:   .byte 0,   16,   16,    16, 16, 0,   16,   16,  16,   32,  8,    8,    8,    8,   8
et_pal:    .byte 0,   1,    2,     3,  0,  0,   2,    1,   3,    3,   1,    3,    3,    1,   3
et_tile_a: .byte 0, SPR_TUTOR, SPR_FRIEND, SPR_MOM, SPR_DAD, 0, SPR_SLIME_A, SPR_WOLF_A, SPR_PL_DOWN_A
           .byte SPR_BOSS_A, SPR_WATER_A, SPR_FIRE_A, SPR_FIRE_A, SPR_POOF_A, SPR_HEART
et_tile_b: .byte 0, SPR_TUTOR, SPR_FRIEND, SPR_MOM, SPR_DAD, 0, SPR_SLIME_B, SPR_WOLF_B, SPR_PL_DOWN_B
           .byte SPR_BOSS_B, SPR_WATER_B, SPR_FIRE_B, SPR_FIRE_B, SPR_POOF_B, SPR_HEART

shot_dx: .byte 0, 0, <-3, 3
shot_dy: .byte 3, <-3, 0, 0

clear_entities:
    ldx #MAX_ENT-1
    lda #0
@l:
    sta e_type,x
    dex
    bpl @l
    rts

; finds a free slot -> X, carry clear; carry set if full. Clears the slot.
ent_alloc:
    ldx #0
@l:
    lda e_type,x
    beq @found
    inx
    cpx #MAX_ENT
    bne @l
    sec
    rts
@found:
    lda #0
    sta e_dir,x
    sta e_timer,x
    sta e_hp,x
    sta e_state,x
    sta e_param,x
    sta e_vx,x
    sta e_vy,x
    sta e_flash,x
    sta e_kb,x
    sta e_kbdir,x
    clc
    rts

spawn_room:
    ldx room
    lda room_spawn_lo,x
    sta ptr0
    lda room_spawn_hi,x
    sta ptr0+1
    ldy #0
@next:
    lda (ptr0),y
    cmp #$FF
    bne @rec
    rts
@rec:
    sta tmp0
    iny
    lda (ptr0),y
    sta tmp1
    iny
    lda (ptr0),y
    sta tmp2
    iny
    lda (ptr0),y
    sta tmp3
    iny
    sty tmp4
    ; conditional spawns
    lda tmp0
    cmp #ET_BULLY
    bne :+
    lda flags
    and #F_BULLIES
    bne @skip
    beq @make
:
    cmp #ET_BOSS
    bne :+
    lda flags
    and #F_BOSS
    bne @skip
    beq @make
:
    cmp #ET_DAD
    bne @make
    lda flags
    and #F_EXAM
    beq @make
    lda #208                    ; stands aside once the exam is given
    sta tmp1
    lda #112
    sta tmp2
@make:
    jsr ent_alloc
    bcs @skip
    lda tmp0
    sta e_type,x
    tay
    lda tmp1
    sta e_x,x
    lda tmp2
    sta e_y,x
    lda tmp3
    sta e_param,x
    lda et_hp,y
    sta e_hp,x
    txa
    asl a
    asl a
    asl a
    clc
    adc #30
    sta e_timer,x
    cpy #ET_WOLF
    bne :+
    lda #1
    sta e_state,x               ; wolves start out resting
:
    cpy #ET_BOSS
    bne @skip
    lda #DIR_RIGHT
    sta e_dir,x
    lda #120
    sta e_timer,x
@skip:
    ldy tmp4
    jmp @next

; ---------------------------------------------------------------- update loop
entities_update:
    ldx #0
@l:
    lda e_type,x
    beq @next
    stx cur_ent
    asl a
    tay
    lda ent_update_table,y
    sta ptr1
    lda ent_update_table+1,y
    sta ptr1+1
    jsr jmp_ptr1
    ldx cur_ent
@next:
    inx
    cpx #MAX_ENT
    bne @l
    rts

ent_update_table:
    .word upd_none, upd_none, upd_none, upd_none, upd_none, upd_none
    .word upd_slime, upd_wolf, upd_bully, upd_boss, upd_shot, upd_shot
    .word upd_efire, upd_poof, upd_heart

upd_none:
    rts

; X = entity, A = dir: try to move 1px. carry set if blocked. Preserves X.
ent_try_move:
    tay
    lda e_x,x
    clc
    adc dir_dx,y
    sta tmp_nx
    lda e_y,x
    clc
    adc dir_dy,y
    sta tmp_ny
    lda tmp_nx
    cmp #241
    bcs @blocked
    lda tmp_ny
    cmp #32
    bcc @blocked
    cmp #225
    bcs @blocked
    stx cur_tmp
    jsr box_blocked
    ldx cur_tmp
    bcs @blocked
    lda tmp_nx
    sta e_x,x
    lda tmp_ny
    sta e_y,x
    clc
    rts
@blocked:
    sec
    rts

; flash timer, contact damage and knockback. carry set while knocked back.
enemy_common:
    lda e_flash,x
    beq :+
    dec e_flash,x
:
    jsr enemy_touch
    lda e_kb,x
    beq @no
    dec e_kb,x
    lda e_kbdir,x
    jsr ent_try_move
    lda e_kbdir,x
    jsr ent_try_move
    sec
    rts
@no:
    clc
    rts

; contact with the player
enemy_touch:
    lda p_inv
    bne @r
    lda p_x
    clc
    adc #3
    sta ax0
    adc #9
    sta ax1
    lda p_y
    clc
    adc #4
    sta ay0
    adc #11
    sta ay1
    lda e_type,x
    cmp #ET_BOSS
    beq @big
    lda e_x,x
    clc
    adc #2
    sta bx0
    adc #11
    sta bx1
    lda e_y,x
    clc
    adc #3
    sta by0
    adc #11
    sta by1
    jmp @test
@big:
    lda e_x,x
    clc
    adc #5
    sta bx0
    adc #21
    sta bx1
    lda e_y,x
    clc
    adc #4
    sta by0
    adc #24
    sta by1
@test:
    jsr overlap
    bcc @r
    ldy e_type,x
    lda et_dmg,y
    jsr hurt_player
    ldx cur_ent
@r:
    rts

aim_at_player:
    lda e_x,x
    sta aim_fx
    lda e_y,x
    sta aim_fy
    lda p_x
    sta aim_tx
    lda p_y
    sta aim_ty
    jmp aim

; ---------------------------------------------------------------- slime
upd_slime:
    jsr enemy_common
    bcs @r
    dec e_timer,x
    bne @move
    jsr rand
    and #3
    sta e_dir,x
    jsr rand
    and #1
    beq @time
    jsr aim_at_player
    sta e_dir,x
@time:
    jsr rand
    and #31
    clc
    adc #30
    sta e_timer,x
@move:
    lda frame
    and #1
    bne @r
    lda e_dir,x
    jsr ent_try_move
    bcc @r
    lda #1
    sta e_timer,x
@r:
    rts

; ---------------------------------------------------------------- wolf
upd_wolf:
    jsr enemy_common
    bcs @r
    lda e_state,x
    bne @rest
    dec e_timer,x
    bne @chase
    lda #1
    sta e_state,x
    lda #45
    sta e_timer,x
    rts
@chase:
    lda frame
    and #3
    beq @r
    jsr aim_at_player
    pha
    jsr ent_try_move
    pla
    bcc @face
    cmp #DIR_LEFT
    bcs @tryv
    lda dx_dir
    jsr ent_try_move
    jmp @face
@tryv:
    lda dy_dir
    jsr ent_try_move
@face:
    lda dx_dir
    sta e_dir,x
@r:
    rts
@rest:
    dec e_timer,x
    bne @r
    lda #0
    sta e_state,x
    lda #100
    sta e_timer,x
    rts

; ---------------------------------------------------------------- bully
upd_bully:
    lda e_state,x
    cmp #2
    beq @flee
    jsr enemy_common
    bcs @r
    dec e_timer,x
    bne @move
    jsr rand
    and #3
    sta e_dir,x
    jsr rand
    and #31
    clc
    adc #20
    sta e_timer,x
@move:
    lda frame
    and #3
    bne @r
    lda e_dir,x
    jsr ent_try_move
    bcc @r
    lda #1
    sta e_timer,x
@r:
    rts
@flee:
    dec e_flash,x
    lda e_y,x
    sec
    sbc #2
    sta e_y,x
    cmp #36
    bcc @gone
    dec e_timer,x
    bne @r
@gone:
    lda #0
    sta e_type,x
    ldy #0
@count:
    lda e_type,y
    cmp #ET_BULLY
    beq @r
    iny
    cpy #MAX_ENT
    bne @count
    lda flags
    ora #F_BULLIES
    sta flags
    ldx #MSG_BULLIES_GONE
    lda #ACT_NONE
    jmp start_talk

; ---------------------------------------------------------------- boss (red wyrm)
; state 0 patrol + fire volleys, 1 dive, 2 climb back, 3 dying
upd_boss:
    lda e_state,x
    cmp #3
    bne :+
    jmp @dying
:
    lda e_flash,x
    beq :+
    dec e_flash,x
:
    jsr enemy_touch
    lda e_state,x
    beq @patrol
    cmp #1
    beq @dive
    ; climb
    dec e_y,x
    lda e_y,x
    cmp #49
    bcs @r
    lda #0
    sta e_state,x
    lda #60
    sta e_timer,x
@r:
    rts
@dive:
    lda e_y,x
    clc
    adc #3
    sta e_y,x
    ; drift toward the player
    lda frame
    and #1
    bne :+
    lda e_x,x
    clc
    adc #8
    cmp p_x
    beq :+
    bcs @dl
    inc e_x,x
    bne :+
@dl:
    dec e_x,x
:
    lda e_y,x
    cmp #150
    bcc @r
    lda #2
    sta e_state,x
    rts
@patrol:
    lda e_hp,x
    cmp #10
    bcc @fastmove               ; enraged below half health
    lda frame
    and #1
    bne @fire
@fastmove:
    lda e_dir,x
    cmp #DIR_LEFT
    bne @right
    dec e_x,x
    lda e_x,x
    cmp #20
    bcs @fire
    lda #DIR_RIGHT
    sta e_dir,x
    bne @fire
@right:
    inc e_x,x
    lda e_x,x
    cmp #204
    bcc @fire
    lda #DIR_LEFT
    sta e_dir,x
@fire:
    dec e_timer,x
    bne @r2
    jsr boss_volley
    ldx cur_ent
    inc e_param,x
    lda e_param,x
    cmp #3
    bcc @again
    lda #0
    sta e_param,x
    lda #1
    sta e_state,x
    lda #60
    sta e_timer,x
    rts
@again:
    lda #80
    ldy e_hp,x
    cpy #10
    bcs :+
    lda #50
:
    sta e_timer,x
@r2:
    rts
@dying:
    dec e_flash,x
    dec e_timer,x
    beq @dead
    lda e_timer,x
    and #7
    bne @r2
    ; explosions
    lda e_x,x
    sta tmp1
    lda e_y,x
    sta tmp2
    jsr ent_alloc
    bcs @r2
    lda #ET_POOF
    sta e_type,x
    jsr rand
    and #15
    clc
    adc #4
    adc tmp1
    sta e_x,x
    jsr rand
    and #15
    clc
    adc #6
    adc tmp2
    sta e_y,x
    lda #16
    sta e_timer,x
    SFX SFX_KILL
    ldx cur_ent
    rts
@dead:
    lda #0
    sta e_type,x
    lda flags
    ora #F_BOSS
    sta flags
    ldx #MSG_BOSS_DOWN
    lda #ACT_BOSS_DOWN
    jmp start_talk

; three fireballs fanned out toward the player
boss_volley:
    lda e_x,x
    clc
    adc #12
    sta aim_fx
    lda e_y,x
    clc
    adc #20
    sta aim_fy
    lda p_x
    clc
    adc #4
    sta aim_tx
    lda p_y
    clc
    adc #6
    sta aim_ty
    jsr aim
    ; tmp0 = major-axis speed on x, tmp1 = on y (signed)
    ; minor axis gets 0/1/2 depending on the angle, then fanned by -1/0/+1
    lda dx_mag
    cmp dy_mag
    bcc @ymajor
    lda dy_mag
    sta tmp4                    ; minor magnitude
    lda dx_mag
    sta tmp5
    jsr minor_speed
    sta tmp1
    lda dy_dir
    cmp #DIR_UP
    bne :+
    lda #0
    sec
    sbc tmp1
    sta tmp1
:
    lda #2
    ldy dx_dir
    cpy #DIR_LEFT
    bne :+
    lda #<-2
:
    sta tmp0
    lda #0
    sta tmp6                    ; fan on y
    jmp @spawn
@ymajor:
    lda dx_mag
    sta tmp4
    lda dy_mag
    sta tmp5
    jsr minor_speed
    sta tmp0
    lda dx_dir
    cmp #DIR_LEFT
    bne :+
    lda #0
    sec
    sbc tmp0
    sta tmp0
:
    lda #2
    ldy dy_dir
    cpy #DIR_UP
    bne :+
    lda #<-2
:
    sta tmp1
    lda #1
    sta tmp6                    ; fan on x
@spawn:
    lda #<-1
    sta tmp7
@shot:
    jsr ent_alloc
    bcs @done
    lda #ET_EFIRE
    sta e_type,x
    lda aim_fx
    sta e_x,x
    lda aim_fy
    sta e_y,x
    lda tmp0
    sta e_vx,x
    lda tmp1
    sta e_vy,x
    lda tmp6
    bne @fanx
    lda tmp1
    clc
    adc tmp7
    sta e_vy,x
    jmp @nextshot
@fanx:
    lda tmp0
    clc
    adc tmp7
    sta e_vx,x
@nextshot:
    inc tmp7
    lda tmp7
    cmp #2
    bne @shot
@done:
    SFX SFX_FIRE
    rts

; tmp4 = minor magnitude, tmp5 = major -> A = 0, 1 or 2
minor_speed:
    lda tmp5
    lsr a
    lsr a
    cmp tmp4
    bcs @zero                   ; minor < major/4
    lda tmp5
    lsr a
    clc
    adc tmp5
    lsr a
    cmp tmp4
    bcs @one                    ; minor < 3/4 major
    lda #2
    rts
@one:
    lda #1
    rts
@zero:
    lda #0
    rts

; ---------------------------------------------------------------- projectiles
upd_shot:
    ldy e_dir,x
    lda e_x,x
    clc
    adc shot_dx,y
    sta e_x,x
    cmp #248
    bcs @kill
    lda e_y,x
    clc
    adc shot_dy,y
    sta e_y,x
    cmp #236
    bcs @kill
    cmp #28
    bcc @kill
    ; walls stop spells (water doesn't)
    lda e_x,x
    clc
    adc #4
    pha
    lda e_y,x
    clc
    adc #4
    tay
    pla
    tax
    jsr tile_at
    ldx cur_ent
    cmp #$FF
    beq @hits
    cmp #MT_WATER
    beq @hits
    tay
    lda mt_solid,y
    beq @hits
    jmp poof_small
@hits:
    jmp shot_hit_enemies
@kill:
    lda #0
    sta e_type,x
    rts

poof_small:
    lda #ET_POOF
    sta e_type,x
    lda #8
    sta e_timer,x
    lda #0
    sta e_param,x
    rts

shot_hit_enemies:
    lda e_x,x
    clc
    adc #1
    sta ax0
    adc #5
    sta ax1
    lda e_y,x
    clc
    adc #1
    sta ay0
    adc #5
    sta ay1
    ldy #0
@l:
    lda e_type,y
    tax
    lda et_flags,x
    and #EF_ENEMY
    beq @next
    cpx #ET_BOSS
    beq @big
    lda e_x,y
    clc
    adc #1
    sta bx0
    adc #13
    sta bx1
    lda e_y,y
    clc
    adc #1
    sta by0
    adc #13
    sta by1
    jmp @test
@big:
    lda e_x,y
    clc
    adc #4
    sta bx0
    adc #23
    sta bx1
    lda e_y,y
    clc
    adc #2
    sta by0
    adc #25
    sta by1
@test:
    jsr overlap
    bcs @hit
@next:
    iny
    cpy #MAX_ENT
    bne @l
    ldx cur_ent
    rts
@hit:
    ldx cur_ent
    lda e_dir,x
    sta tmp5                    ; knockback direction
    lda e_type,x
    cmp #ET_WSHOT
    beq @water
    lda #3
    bne @dmg
@water:
    lda #1
@dmg:
    jsr enemy_hurt
    ldx cur_ent
    jmp poof_small

; Y = enemy, A = damage, tmp5 = knockback dir
enemy_hurt:
    sta tmp0
    lda e_flash,y
    beq :+
    rts
:
    lda e_type,y
    cmp #ET_BULLY
    bne :+
    lda e_state,y
    cmp #2
    bne :+
    rts
:
    lda e_type,y
    cmp #ET_BOSS
    bne :+
    lda e_state,y
    cmp #3
    bne :+
    rts
:
    lda e_hp,y
    sec
    sbc tmp0
    sta e_hp,y
    beq @dead
    bcc @dead
    lda #12
    sta e_flash,y
    lda e_type,y
    cmp #ET_BOSS
    beq @snd
    lda #6
    sta e_kb,y
    lda tmp5
    sta e_kbdir,y
@snd:
    SFX SFX_HIT
    rts
@dead:
    lda e_type,y
    cmp #ET_BULLY
    bne @notbully
    lda #2
    sta e_state,y
    lda #40
    sta e_timer,y
    sta e_flash,y
    SFX SFX_HIT
    rts
@notbully:
    cmp #ET_BOSS
    bne @plain
    lda #3
    sta e_state,y
    lda #140
    sta e_timer,y
    sta e_flash,y
    SFX SFX_KILL
    rts
@plain:
    lda #ET_POOF
    sta e_type,y
    lda #16
    sta e_timer,y
    lda e_x,y
    clc
    adc #4
    sta e_x,y
    lda e_y,y
    clc
    adc #4
    sta e_y,y
    lda #0
    sta e_param,y
    jsr rand
    and #3
    bne :+
    lda #1
    sta e_param,y               ; drop a heart
:
    SFX SFX_KILL
    rts

upd_efire:
    lda e_x,x
    clc
    adc e_vx,x
    sta e_x,x
    cmp #248
    bcs @kill
    cmp #4
    bcc @kill
    lda e_y,x
    clc
    adc e_vy,x
    sta e_y,x
    cmp #236
    bcs @kill
    cmp #32
    bcc @kill
    lda p_inv
    bne @r
    lda p_x
    clc
    adc #3
    sta ax0
    adc #9
    sta ax1
    lda p_y
    clc
    adc #4
    sta ay0
    adc #11
    sta ay1
    lda e_x,x
    clc
    adc #1
    sta bx0
    adc #5
    sta bx1
    lda e_y,x
    clc
    adc #1
    sta by0
    adc #5
    sta by1
    jsr overlap
    bcc @r
    lda #1
    jsr hurt_player
    ldx cur_ent
@kill:
    lda #0
    sta e_type,x
@r:
    rts

upd_poof:
    dec e_timer,x
    bne @r
    lda e_param,x
    beq @gone
    lda #ET_HEART
    sta e_type,x
    lda #0
    sta e_param,x
    lda #255
    sta e_timer,x
    rts
@gone:
    lda #0
    sta e_type,x
@r:
    rts

upd_heart:
    dec e_timer,x
    beq @gone
    lda p_x
    clc
    adc #2
    sta ax0
    adc #12
    sta ax1
    lda p_y
    clc
    adc #2
    sta ay0
    adc #13
    sta ay1
    lda e_x,x
    sta bx0
    clc
    adc #7
    sta bx1
    lda e_y,x
    sta by0
    clc
    adc #7
    sta by1
    jsr overlap
    bcc @r
    lda p_hp
    cmp p_hpmax
    bcs :+
    inc p_hp
:
    lda hud_dirty
    ora #1
    sta hud_dirty
    SFX SFX_PICKUP
@gone:
    lda #0
    sta e_type,x
@r:
    rts

; ---------------------------------------------------------------- drawing
; X = entity
draw_entity:
    lda e_type,x
    bne :+
    rts
:
    tay
    lda et_size,y
    bne :+
    rts
:
    sta tmp7
    ; blink while hurt / fading
    lda e_flash,x
    and #2
    beq :+
    rts
:
    cpy #ET_HEART
    bne :+
    lda e_timer,x
    cmp #64
    bcs :+
    lda frame
    and #4
    beq :+
    rts
:
    lda e_x,x
    sta spr_x
    lda e_y,x
    sta spr_y
    lda et_pal,y
    sta spr_attr
    ; animation frame
    lda frame
    cpy #ET_POOF
    bne @notpoof
    lda e_timer,x
    and #8
    eor #8
    jmp @pick
@notpoof:
    cpy #ET_BOSS
    bne :+
    lsr a
:
    lsr a
    lsr a
    lsr a
    and #1
@pick:
    beq @a
    lda et_tile_b,y
    bne @tile
@a:
    lda et_tile_a,y
@tile:
    sta spr_tile
    cpy #ET_WOLF
    bne :+
    lda e_dir,x
    cmp #DIR_LEFT
    bne :+
    lda spr_attr
    ora #$40
    sta spr_attr
:
    lda tmp7
    cmp #16
    beq @d16
    cmp #8
    beq @d8
    jmp draw32
@d16:
    jmp draw16
@d8:
    jmp draw8
