// Level maps. Like Prince of Persia, a screen ("room") is 10 cells wide and 3 cells tall,
// and the camera flips from room to room. Each map row is one row of cells.
//
//  .  air             _  floor            #  stone block        |  pillar (floor)    t  torch (floor)
//  ^  spikes          ~  loose floor      x  blades             r  falling rocks land here (floor)
//  a-c pressure plate that opens gate A-C                       w  water (drowns)
//  o  target (shoot it)   O  bridge, appears when the target is hit
//  h  small potion    H  big potion       *  Shahnameh leaf     F  fire altar (checkpoint)
//  S  start           E  exit door        M  the ox-head mace   1-9 hint
//  g  guard           K  Karkoy (boss)    D  the dragon (boss)
var SAM = window.SAM || (window.SAM = {});

SAM.LEVELS = [
  {
    id: 1, name: 'lv1', theme: 'fort', enemy: 'bandit', intro: 'intro1', outro: 'outro1',
    music: 'calm', needMace: true,
    map: [
      '##################################################',
      '#.....____' + '__3_..___*' + '_4^_~__5..' + '##########' + '##########',
      '#S1_t2___#' + '#__*_____#' + '#*________' + '_a__A_6_M_' + '__7_g__hE#',
    ],
  },
  {
    id: 2, name: 'lv2', theme: 'cave', enemy: 'bandit', intro: 'intro2', outro: 'outro2',
    music: 'calm', bow: true,
    map: [
      '##########' + '##########' + '##########' + '##########' + '..........',
      '#.........' + '..........' + '...____*..' + '..........' + '_*........',
      '#S_1_OOOo_' + '_~~_ww2r__' + '_h____ww__' + 'F__t___*__' + '_____Dwwww',
    ],
  },
  {
    id: 3, name: 'lv3', theme: 'alborz', enemy: 'bandit', intro: 'intro3', outro: 'outro3',
    music: 'calm', bow: true,
    map: [
      '...___*___.._____hE#',
      '#____..............#',
      '#####_2_r__r_~__...#',
      '#...............F__#',
      '#.._*^_____..___...#',
      '#___...............#',
      '####__h_~__^____...#',
      '#......______..___.#',
      '#S1______________*_#',
    ],
  },
  {
    id: 4, name: 'lv4', theme: 'dungeon', enemy: 'div', intro: 'intro4', outro: 'outro4',
    music: 'battle', bow: true, feather: true,
    map: [
      '############################################################',
      '#.........' + '....__a_..' + '..........' + '..........' + '.b_*......' + '..........',
      '#S1t_x_t__' + '__g__h__A_' + '__~~*^_H__' + 'F*_g_x____' + '___..__g_B' + 'Fh_2___K_#',
    ],
  },
];
