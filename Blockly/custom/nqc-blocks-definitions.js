// ============================================================================
// Definições de Blocos Customizados NQC
// ============================================================================
// Este arquivo contém as definições de interface dos blocos.
// Os geradores de código estão em nqc-blocks-generators.js
// ============================================================================

// ============ SISTEMA DE NOMENCLATURA DUAL ============

/**
 * Modo de nomenclatura atual ('iniciante' ou 'tecnico')
 * @type {string}
 */
var nomenclaturaAtual = 'iniciante';

/**
 * Dicionário de nomenclaturas para cada bloco
 * Estrutura: { iniciante: { blockType: 'texto' }, tecnico: { blockType: 'texto' } }
 */
const NOMENCLATURAS = {
    iniciante: {
        // SEÇÃO 1: MOTORES E SONS
        'nqc_ligar_motor_com_potencia': 'ligar motores',
        'nqc_ligar_motor': 'ligar motores',
        'nqc_desligar_motor': 'desligar motores',
        'nqc_define_potencia_percent': 'define potência',
        'nqc_define_sentido': 'define sentido',
        'nqc_toca_som': 'toca som',

        // SEÇÃO 2: SENSORES
        'nqc_define_sensor_toque': 'define que',
        'nqc_define_sensor_luz': 'define que',
        'nqc_define_sensor_rotacao': 'define que',
        'nqc_define_sensor_temperatura': 'define que',
        'nqc_valor_sensor_toque': 'valor do sensor de toque',
        'nqc_valor_sensor_luz': 'valor do sensor de luz',
        'nqc_valor_sensor_rotacao': 'valor do sensor de rotação',
        'nqc_valor_sensor_temperatura': 'temperatura em °C do sensor',

        // SEÇÃO 3: TEMPORIZAÇÃO E LOOPS
        'nqc_espera_segundos': 'espera',
        'nqc_espera_ate_que': 'espere até que',
        'nqc_repita_vezes': 'repita',
        'nqc_repita_infinitamente': 'repita infinitamente',
        'nqc_repita_ate_que': 'repita até que',

        // SEÇÃO 4: VARIÁVEIS
        'nqc_variavel_recebe': 'variável',
        'nqc_valor_variavel': 'valor de',

        // SEÇÃO 5: MATEMÁTICA
        'nqc_numero': '',
        'nqc_percentual': '',
        'nqc_operacao_matematica': '',

        // SEÇÃO 6: LÓGICA
        'nqc_comparacao': '',
        'nqc_operacao_logica': '',
        'nqc_contrario': 'contrário de',
        'nqc_booleano': '',

        // SEÇÃO 7: CONDICIONAIS
        'nqc_se_faca': 'se',
        'nqc_se_faca_senao': 'se',

        // SEÇÃO 8: TAREFAS
        'nqc_tarefa_principal': 'tarefa principal',
        'nqc_tarefa_nomeada': 'tarefa',
        'nqc_executar_tarefa': 'executar tarefa',
        'nqc_interromper_tarefa': 'interromper tarefa'
    },
    tecnico: {
        // SEÇÃO 1: MOTORES E SONS
        'nqc_ligar_motor_com_potencia': 'OnFwd/OnRev + SetPower',
        'nqc_ligar_motor': 'OnFwd/OnRev',
        'nqc_desligar_motor': 'Off',
        'nqc_define_potencia_percent': 'SetPower',
        'nqc_define_sentido': 'SetDirection',
        'nqc_toca_som': 'PlaySound',

        // SEÇÃO 2: SENSORES
        'nqc_define_sensor_toque': 'SetSensorType (TOUCH)',
        'nqc_define_sensor_luz': 'SetSensorType (LIGHT)',
        'nqc_define_sensor_rotacao': 'SetSensorType (ROTATION)',
        'nqc_define_sensor_temperatura': 'SetSensorType (TEMP)',
        'nqc_valor_sensor_toque': 'SENSOR (TOUCH)',
        'nqc_valor_sensor_luz': 'SENSOR (LIGHT)',
        'nqc_valor_sensor_rotacao': 'SENSOR (ROTATION)',
        'nqc_valor_sensor_temperatura': 'SENSOR (CELSIUS)',

        // SEÇÃO 3: TEMPORIZAÇÃO E LOOPS
        'nqc_espera_segundos': 'Wait',
        'nqc_espera_ate_que': 'until',
        'nqc_repita_vezes': 'repeat',
        'nqc_repita_infinitamente': 'while(true)',
        'nqc_repita_ate_que': 'do...until',

        // SEÇÃO 4: VARIÁVEIS
        'nqc_variavel_recebe': 'int',
        'nqc_valor_variavel': 'var',

        // SEÇÃO 5: MATEMÁTICA
        'nqc_numero': '',
        'nqc_percentual': '',
        'nqc_operacao_matematica': '',

        // SEÇÃO 6: LÓGICA
        'nqc_comparacao': '',
        'nqc_operacao_logica': '',
        'nqc_contrario': '!',
        'nqc_booleano': '',

        // SEÇÃO 7: CONDICIONAIS
        'nqc_se_faca': 'if',
        'nqc_se_faca_senao': 'if...else',

        // SEÇÃO 8: TAREFAS
        'nqc_tarefa_principal': 'task main',
        'nqc_tarefa_nomeada': 'task',
        'nqc_executar_tarefa': 'start',
        'nqc_interromper_tarefa': 'stop'
    }
};

/**
 * Obtém o texto de um bloco baseado na nomenclatura atual
 * @param {string} blockType - Tipo do bloco
 * @return {string} - Texto a ser exibido
 */
function getTextoBloco(blockType) {
    return NOMENCLATURAS[nomenclaturaAtual][blockType] || NOMENCLATURAS['iniciante'][blockType] || '';
}

// ============ CONSTANTES REUTILIZÁVEIS ============

const SENSOR_OPTIONS = [
    ["1", "SENSOR_1"],
    ["2", "SENSOR_2"],
    ["3", "SENSOR_3"]
];

const MOTOR_OPTIONS = [
    ["A", "OUT_A"],
    ["B", "OUT_B"],
    ["C", "OUT_C"],
    ["A+B", "OUT_A+OUT_B"],
    ["A+C", "OUT_A+OUT_C"],
    ["A+B+C", "OUT_A+OUT_B+OUT_C"]
];

const SENTIDO_OPTIONS = [
    ["horário", "FWD"],
    ["antihorário", "REV"]
];

// ============ FUNÇÃO DE VALIDAÇÃO REUTILIZÁVEL ============

/**
 * Função reutilizável para validação de blocos de tarefa
 * @param {Blockly.Block} block - Bloco a ser validado
 * @param {string} taskType - Tipo da tarefa ('nqc_tarefa_principal' ou 'nqc_tarefa_nomeada')
 */
function validateTaskBlock(block, taskType) {
    if (!block.workspace) return;

    const blocks = block.workspace.getBlocksByType(taskType, false);

    if (taskType === 'nqc_tarefa_principal') {
        // Validação de tarefa única
        if (blocks.length > 1 && block.id !== blocks[0].id) {
            block.setWarningText('Apenas um bloco de "tarefa principal" é permitido no projeto. Remova este bloco.');
            block.disabled = true;
        } else {
            block.setWarningText(null);
            block.disabled = false;
        }
    } else if (taskType === 'nqc_tarefa_nomeada') {
        // Validação de nome único
        const currentName = block.getFieldValue('NOME');
        if (!currentName || currentName.trim() === '') {
            block.setWarningText('O nome da tarefa não pode estar vazio.');
            block.disabled = true;
            return;
        }

        const duplicates = blocks.filter(b => 
            b.id !== block.id && b.getFieldValue('NOME') === currentName
        );

        if (duplicates.length > 0) {
            block.setWarningText('Já existe uma tarefa com o nome "' + currentName + '". Escolha um nome diferente.');
            block.disabled = true;
        } else {
            block.setWarningText(null);
            block.disabled = false;
        }
    }
}

// ============ SEÇÃO 1: MOTORES E SONS ============

Blockly.Blocks['nqc_ligar_motor_com_potencia'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_ligar_motor_com_potencia'))
            .appendField(new Blockly.FieldDropdown(MOTOR_OPTIONS), "MOTOR");
        this.appendDummyInput()
            .appendField("no sentido")
            .appendField(new Blockly.FieldDropdown(SENTIDO_OPTIONS), "SENTIDO");
        this.appendValueInput("POTENCIA")
            .setCheck("Percent")
            .appendField("com potência");
        this.setInputsInline(false);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(210);
        this.setTooltip("Liga motores com direção e potência especificadas");
    }
};

Blockly.Blocks['nqc_ligar_motor'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_ligar_motor'))
            .appendField(new Blockly.FieldDropdown(MOTOR_OPTIONS), "MOTOR");
        this.appendDummyInput()
            .appendField("no sentido")
            .appendField(new Blockly.FieldDropdown(SENTIDO_OPTIONS), "SENTIDO");
        this.setInputsInline(false);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(210);
        this.setTooltip("Liga motores com direção especificada");
    }
};

Blockly.Blocks['nqc_desligar_motor'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_desligar_motor'))
            .appendField(new Blockly.FieldDropdown(MOTOR_OPTIONS), "MOTOR");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(210);
        this.setTooltip("Desliga os motores selecionados");
    }
};

Blockly.Blocks['nqc_define_potencia_percent'] = {
    init: function () {
        this.appendValueInput("POTENCIA")
            .setCheck("Percent")
            .appendField(getTextoBloco('nqc_define_potencia_percent'));
        this.appendDummyInput()
            .appendField("para motores")
            .appendField(new Blockly.FieldDropdown(MOTOR_OPTIONS), "MOTOR");
        this.setInputsInline(false);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(210);
        this.setTooltip("Define a potência dos motores em percentual");
    }
};

Blockly.Blocks['nqc_define_sentido'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_define_sentido'))
            .appendField(new Blockly.FieldDropdown(SENTIDO_OPTIONS), "SENTIDO");
        this.appendDummyInput()
            .appendField("para motores")
            .appendField(new Blockly.FieldDropdown(MOTOR_OPTIONS), "MOTOR");
        this.setInputsInline(false);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(210);
        this.setTooltip("Define a direção dos motores");
    }
};

Blockly.Blocks['nqc_toca_som'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_toca_som'))
            .appendField(new Blockly.FieldDropdown([
                ["Click", "SOUND_CLICK"],
                ["Beep duplo", "SOUND_DOUBLE_BEEP"],
                ["Descendo", "SOUND_DOWN"],
                ["Subindo", "SOUND_UP"],
                ["Beep baixo", "SOUND_LOW_BEEP"],
                ["Subida rápida", "SOUND_FAST_UP"]
            ]), "SOM");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(210);
        this.setTooltip("Toca um som pré-definido");
    }
};

// ============ SEÇÃO 2: SENSORES ============

Blockly.Blocks['nqc_define_sensor_toque'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_define_sensor_toque'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR")
            .appendField("é um sensor de toque");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(140);
        this.setTooltip("Define um sensor como sensor de toque");
    }
};

Blockly.Blocks['nqc_define_sensor_luz'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_define_sensor_luz'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR")
            .appendField("é um sensor de luz");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(140);
        this.setTooltip("Define um sensor como sensor de luz");
    }
};

Blockly.Blocks['nqc_define_sensor_rotacao'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_define_sensor_rotacao'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR")
            .appendField("é um sensor de rotação");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(140);
        this.setTooltip("Define um sensor como sensor de rotação");
    }
};

Blockly.Blocks['nqc_define_sensor_temperatura'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_define_sensor_temperatura'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR")
            .appendField("é um sensor de temperatura");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(140);
        this.setTooltip("Define um sensor como sensor de temperatura");
    }
};

Blockly.Blocks['nqc_valor_sensor_toque'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_valor_sensor_toque'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR");
        this.setOutput(true, "Boolean");
        this.setColour(140);
        this.setTooltip("Retorna o valor do sensor de toque (verdadeiro se pressionado)");
    }
};

Blockly.Blocks['nqc_valor_sensor_luz'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_valor_sensor_luz'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR");
        this.setOutput(true, "Number");
        this.setColour(140);
        this.setTooltip("Retorna o valor do sensor de luz (0-100)");
    }
};

Blockly.Blocks['nqc_valor_sensor_rotacao'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_valor_sensor_rotacao'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR");
        this.setOutput(true, "Number");
        this.setColour(140);
        this.setTooltip("Retorna o valor do sensor de rotação");
    }
};

Blockly.Blocks['nqc_valor_sensor_temperatura'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_valor_sensor_temperatura'))
            .appendField(new Blockly.FieldDropdown(SENSOR_OPTIONS), "SENSOR");
        this.setOutput(true, "Number");
        this.setColour(140);
        this.setTooltip("Retorna a temperatura em graus Celsius do sensor de temperatura");
    }
};

// ============ SEÇÃO 3: TEMPORIZAÇÃO E LOOPS ============

Blockly.Blocks['nqc_espera_segundos'] = {
    init: function () {
        this.appendValueInput("SECONDS")
            .setCheck("Number")
            .appendField(getTextoBloco('nqc_espera_segundos'));
        this.appendDummyInput()
            .appendField("segundos");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(90);
        this.setTooltip("Espera o número especificado de segundos");
    }
};

Blockly.Blocks['nqc_espera_ate_que'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField(getTextoBloco('nqc_espera_ate_que'));
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(90);
        this.setTooltip("Espera até que a condição seja verdadeira");
    }
};

Blockly.Blocks['nqc_repita_vezes'] = {
    init: function () {
        this.appendValueInput("TIMES")
            .setCheck("Number")
            .appendField(getTextoBloco('nqc_repita_vezes'));
        this.appendDummyInput()
            .appendField("vezes");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(90);
        this.setTooltip("Repete os comandos internos N vezes");
    }
};

Blockly.Blocks['nqc_repita_infinitamente'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_repita_infinitamente'));
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(90);
        this.setTooltip("Repete os comandos internos infinitamente");
    }
};

Blockly.Blocks['nqc_repita_ate_que'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField(getTextoBloco('nqc_repita_ate_que'));
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(90);
        this.setTooltip("Repete os comandos até que a condição seja verdadeira");
    }
};

// ============ SEÇÃO 4: VARIÁVEIS ============

Blockly.Blocks['nqc_variavel_recebe'] = {
    init: function () {
        this.appendValueInput("VALOR")
            .setCheck("Number")
            .appendField(getTextoBloco('nqc_variavel_recebe'))
            .appendField(new Blockly.FieldTextInput("x"), "VAR")
            .appendField("recebe");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(0);
        this.setTooltip("Atribui um valor a uma variável");
    }
};

Blockly.Blocks['nqc_valor_variavel'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_valor_variavel'))
            .appendField(new Blockly.FieldTextInput("x"), "VAR");
        this.setOutput(true, "Number");
        this.setColour(0);
        this.setTooltip("Retorna o valor de uma variável");
    }
};

// ============ SEÇÃO 5: MATEMÁTICA ============

Blockly.Blocks['nqc_numero'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(new Blockly.FieldNumber(0), "NUM");
        this.setOutput(true, "Number");
        this.setColour(330);
        this.setTooltip("Um número");
    }
};

Blockly.Blocks['nqc_percentual'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(new Blockly.FieldNumber(50, 0, 100), "NUM")
            .appendField("%");
        this.setOutput(true, "Percent");
        this.setColour(210);
        this.setTooltip("Um valor percentual entre 0 e 100");
    }
};

Blockly.Blocks['nqc_operacao_matematica'] = {
    init: function () {
        this.appendValueInput("A")
            .setCheck("Number");
        this.appendDummyInput()
            .appendField(new Blockly.FieldDropdown([
                ["mais", "ADD"],
                ["menos", "MINUS"],
                ["vezes", "MULTIPLY"],
                ["dividido por", "DIVIDE"]
            ]), "OP");
        this.appendValueInput("B")
            .setCheck("Number");
        this.setInputsInline(true);
        this.setOutput(true, "Number");
        this.setColour(330);
        this.setTooltip("Operação matemática entre dois valores");
        this.getInput("A").connection.setShadowDom(Blockly.utils.xml.textToDom('<shadow type="nqc_numero"><field name="NUM">0</field></shadow>'));
        this.getInput("B").connection.setShadowDom(Blockly.utils.xml.textToDom('<shadow type="nqc_numero"><field name="NUM">0</field></shadow>'));
    }
};

// ============ SEÇÃO 6: LÓGICA ============

Blockly.Blocks['nqc_comparacao'] = {
    init: function () {
        this.appendValueInput("A")
            .setCheck("Number");
        this.appendDummyInput()
            .appendField(new Blockly.FieldDropdown([
                ["igual a", "EQ"],
                ["diferente de", "NEQ"],
                ["menor do que", "LT"],
                ["menor ou igual a", "LTE"],
                ["maior do que", "GT"],
                ["maior ou igual a", "GTE"]
            ]), "OP");
        this.appendValueInput("B")
            .setCheck("Number");
        this.setInputsInline(true);
        this.setOutput(true, "Boolean");
        this.setColour(290);
        this.setTooltip("Comparação entre dois valores");
        this.getInput("A").connection.setShadowDom(Blockly.utils.xml.textToDom('<shadow type="nqc_numero"><field name="NUM">0</field></shadow>'));
        this.getInput("B").connection.setShadowDom(Blockly.utils.xml.textToDom('<shadow type="nqc_numero"><field name="NUM">0</field></shadow>'));
    }
};

Blockly.Blocks['nqc_operacao_logica'] = {
    init: function () {
        this.appendValueInput("A")
            .setCheck("Boolean");
        this.appendDummyInput()
            .appendField(new Blockly.FieldDropdown([
                ["e", "AND"],
                ["ou", "OR"]
            ]), "OP");
        this.appendValueInput("B")
            .setCheck("Boolean");
        this.setInputsInline(true);
        this.setOutput(true, "Boolean");
        this.setColour(290);
        this.setTooltip("Operação lógica entre dois valores booleanos");
    }
};

Blockly.Blocks['nqc_contrario'] = {
    init: function () {
        this.appendValueInput("BOOL")
            .setCheck("Boolean")
            .appendField(getTextoBloco('nqc_contrario'));
        this.setInputsInline(true);
        this.setOutput(true, "Boolean");
        this.setColour(290);
        this.setTooltip("Inverte o valor booleano");
    }
};

Blockly.Blocks['nqc_booleano'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(new Blockly.FieldDropdown([
                ["verdadeiro", "TRUE"],
                ["falso", "FALSE"]
            ]), "BOOL");
        this.setOutput(true, "Boolean");
        this.setColour(290);
        this.setTooltip("Valor booleano");
    }
};

// ============ SEÇÃO 7: CONDICIONAIS ============

Blockly.Blocks['nqc_se_faca'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField(getTextoBloco('nqc_se_faca'));
        this.appendDummyInput()
            .appendField(", faça");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(260);
        this.setTooltip("Executa comandos se a condição for verdadeira");
    }
};

Blockly.Blocks['nqc_se_faca_senao'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField(getTextoBloco('nqc_se_faca_senao'));
        this.appendDummyInput()
            .appendField(", faça");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.appendDummyInput()
            .appendField("senão, faça");
        this.appendStatementInput("ELSE")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(260);
        this.setTooltip("Executa comandos se a condição for verdadeira, senão executa outros comandos");
    }
};

// ============ SEÇÃO 8: TAREFAS ============

Blockly.Blocks['nqc_tarefa_principal'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_tarefa_principal'));
        this.appendStatementInput("STATEMENTS")
            .setCheck(null);
        this.setColour(170);
        this.setTooltip("Tarefa principal do programa (task main)");
    },

    onchange: function(event) {
        validateTaskBlock(this, 'nqc_tarefa_principal');
    }
};

Blockly.Blocks['nqc_tarefa_nomeada'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_tarefa_nomeada'))
            .appendField(new Blockly.FieldTextInput("minhaTarefa"), "NOME");
        this.appendStatementInput("STATEMENTS")
            .setCheck(null);
        this.setColour(170);
        this.setTooltip("Tarefa com nome personalizado");
    },

    onchange: function(event) {
        validateTaskBlock(this, 'nqc_tarefa_nomeada');
    }
};

Blockly.Blocks['nqc_executar_tarefa'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_executar_tarefa'))
            .appendField(new Blockly.FieldTextInput("minhaTarefa"), "NOME");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(170);
        this.setTooltip("Inicia a execução de uma tarefa");
    }
};

Blockly.Blocks['nqc_interromper_tarefa'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(getTextoBloco('nqc_interromper_tarefa'))
            .appendField(new Blockly.FieldTextInput("minhaTarefa"), "NOME");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(170);
        this.setTooltip("Interrompe a execução de uma tarefa");
    }
};

console.log('[NQC-BLOCKS-DEFINITIONS] Definições de blocos NQC carregadas');
