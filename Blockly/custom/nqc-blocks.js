// ============ SEÇÃO 1: MOTORES E SONS ============

Blockly.Blocks['nqc_ligar_motor_com_potencia'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("ligar motores")
            .appendField(new Blockly.FieldDropdown([
                ["A", "OUT_A"],
                ["B", "OUT_B"],
                ["C", "OUT_C"],
                ["A+B", "OUT_A+OUT_B"],
                ["A+C", "OUT_A+OUT_C"],
                ["A+B+C", "OUT_A+OUT_B+OUT_C"]
            ]), "MOTOR")
            .appendField("no sentido")
            .appendField(new Blockly.FieldDropdown([
                ["horário", "FWD"],
                ["antihorário", "REV"]
            ]), "SENTIDO")
            .appendField("com potência");
        this.appendValueInput("POTENCIA")
            .setCheck("Number");
        this.appendDummyInput()
            .appendField("por cento");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(160);
        this.setTooltip("Liga motores com direção e potência especificadas");
    }
};

Blockly.Blocks['nqc_ligar_motor'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("ligar motores")
            .appendField(new Blockly.FieldDropdown([
                ["A", "OUT_A"],
                ["B", "OUT_B"],
                ["C", "OUT_C"],
                ["A+B", "OUT_A+OUT_B"],
                ["A+C", "OUT_A+OUT_C"],
                ["A+B+C", "OUT_A+OUT_B+OUT_C"]
            ]), "MOTOR")
            .appendField("no sentido")
            .appendField(new Blockly.FieldDropdown([
                ["horário", "FWD"],
                ["antihorário", "REV"]
            ]), "SENTIDO");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(160);
        this.setTooltip("Liga motores com direção especificada");
    }
};

Blockly.Blocks['nqc_define_potencia_percent'] = {
    init: function () {
        this.appendValueInput("POTENCIA")
            .setCheck("Number")
            .appendField("define potência");
        this.appendDummyInput()
            .appendField("por cento para motores")
            .appendField(new Blockly.FieldDropdown([
                ["A", "OUT_A"],
                ["B", "OUT_B"],
                ["C", "OUT_C"],
                ["A+B", "OUT_A+OUT_B"],
                ["A+C", "OUT_A+OUT_C"],
                ["A+B+C", "OUT_A+OUT_B+OUT_C"]
            ]), "MOTOR");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(160);
        this.setTooltip("Define a potência dos motores em percentual");
    }
};

Blockly.Blocks['nqc_define_sentido'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("define sentido")
            .appendField(new Blockly.FieldDropdown([
                ["horário", "FWD"],
                ["antihorário", "REV"]
            ]), "SENTIDO")
            .appendField("para motores")
            .appendField(new Blockly.FieldDropdown([
                ["A", "OUT_A"],
                ["B", "OUT_B"],
                ["C", "OUT_C"],
                ["A+B", "OUT_A+OUT_B"],
                ["A+C", "OUT_A+OUT_C"],
                ["A+B+C", "OUT_A+OUT_B+OUT_C"]
            ]), "MOTOR");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(160);
        this.setTooltip("Define a direção dos motores");
    }
};

Blockly.Blocks['nqc_toca_som'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("toca som")
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
        this.setColour(160);
        this.setTooltip("Toca um som pré-definido");
    }
};

// ============ SEÇÃO 2: SENSORES ============

Blockly.Blocks['nqc_define_sensor_toque'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("define que")
            .appendField(new Blockly.FieldDropdown([
                ["1", "SENSOR_1"],
                ["2", "SENSOR_2"],
                ["3", "SENSOR_3"]
            ]), "SENSOR")
            .appendField("é um sensor de toque");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(200);
        this.setTooltip("Define um sensor como sensor de toque");
    }
};

Blockly.Blocks['nqc_define_sensor_luz'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("define que")
            .appendField(new Blockly.FieldDropdown([
                ["1", "SENSOR_1"],
                ["2", "SENSOR_2"],
                ["3", "SENSOR_3"]
            ]), "SENSOR")
            .appendField("é um sensor de luz");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(200);
        this.setTooltip("Define um sensor como sensor de luz");
    }
};

Blockly.Blocks['nqc_define_sensor_rotacao'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("define que")
            .appendField(new Blockly.FieldDropdown([
                ["1", "SENSOR_1"],
                ["2", "SENSOR_2"],
                ["3", "SENSOR_3"]
            ]), "SENSOR")
            .appendField("é um sensor de rotação");
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(200);
        this.setTooltip("Define um sensor como sensor de rotação");
    }
};

Blockly.Blocks['nqc_valor_sensor_toque'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("valor do sensor de toque")
            .appendField(new Blockly.FieldDropdown([
                ["1", "SENSOR_1"],
                ["2", "SENSOR_2"],
                ["3", "SENSOR_3"]
            ]), "SENSOR");
        this.setOutput(true, "Boolean");
        this.setColour(200);
        this.setTooltip("Retorna o valor do sensor de toque (verdadeiro se pressionado)");
    }
};

Blockly.Blocks['nqc_valor_sensor_luz'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("valor do sensor de luz")
            .appendField(new Blockly.FieldDropdown([
                ["1", "SENSOR_1"],
                ["2", "SENSOR_2"],
                ["3", "SENSOR_3"]
            ]), "SENSOR");
        this.setOutput(true, "Number");
        this.setColour(200);
        this.setTooltip("Retorna o valor do sensor de luz (0-100)");
    }
};

Blockly.Blocks['nqc_valor_sensor_rotacao'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("valor do sensor de rotação")
            .appendField(new Blockly.FieldDropdown([
                ["1", "SENSOR_1"],
                ["2", "SENSOR_2"],
                ["3", "SENSOR_3"]
            ]), "SENSOR");
        this.setOutput(true, "Number");
        this.setColour(200);
        this.setTooltip("Retorna o valor do sensor de rotação");
    }
};

// ============ SEÇÃO 3: TEMPORIZAÇÃO E LOOPS ============

Blockly.Blocks['nqc_espera_segundos'] = {
    init: function () {
        this.appendValueInput("SECONDS")
            .setCheck("Number")
            .appendField("espera");
        this.appendDummyInput()
            .appendField("segundos");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(120);
        this.setTooltip("Espera o número especificado de segundos");
    }
};

Blockly.Blocks['nqc_espera_ate_que'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField("espere até que");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(120);
        this.setTooltip("Espera até que a condição seja verdadeira");
    }
};

Blockly.Blocks['nqc_repita_vezes'] = {
    init: function () {
        this.appendValueInput("TIMES")
            .setCheck("Number")
            .appendField("repita");
        this.appendDummyInput()
            .appendField("vezes");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(120);
        this.setTooltip("Repete os comandos internos N vezes");
    }
};

Blockly.Blocks['nqc_repita_infinitamente'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("repita infinitamente");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(120);
        this.setTooltip("Repete os comandos internos infinitamente");
    }
};

Blockly.Blocks['nqc_repita_ate_que'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField("repita até que");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(120);
        this.setTooltip("Repete os comandos até que a condição seja verdadeira");
    }
};

// ============ SEÇÃO 4: VARIÁVEIS ============

Blockly.Blocks['nqc_variavel_recebe'] = {
    init: function () {
        this.appendValueInput("VALOR")
            .setCheck("Number")
            .appendField("variável")
            .appendField(new Blockly.FieldTextInput("x"), "VAR")
            .appendField("recebe");
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(330);
        this.setTooltip("Atribui um valor a uma variável");
    }
};

Blockly.Blocks['nqc_valor_variavel'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("valor de")
            .appendField(new Blockly.FieldTextInput("x"), "VAR");
        this.setOutput(true, "Number");
        this.setColour(330);
        this.setTooltip("Retorna o valor de uma variável");
    }
};

// ============ SEÇÃO 5: MATEMÁTICA ============

Blockly.Blocks['nqc_numero'] = {
    init: function () {
        this.appendDummyInput()
            .appendField(new Blockly.FieldNumber(0), "NUM");
        this.setOutput(true, "Number");
        this.setColour(230);
        this.setTooltip("Um número");
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
        this.setColour(230);
        this.setTooltip("Operação matemática entre dois valores");
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
        this.setColour(210);
        this.setTooltip("Comparação entre dois valores");
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
        this.setColour(210);
        this.setTooltip("Operação lógica entre dois valores booleanos");
    }
};

Blockly.Blocks['nqc_contrario'] = {
    init: function () {
        this.appendValueInput("BOOL")
            .setCheck("Boolean")
            .appendField("contrário de");
        this.setInputsInline(true);
        this.setOutput(true, "Boolean");
        this.setColour(210);
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
        this.setColour(210);
        this.setTooltip("Valor booleano");
    }
};

// ============ SEÇÃO 7: CONDICIONAIS ============

Blockly.Blocks['nqc_se_faca'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField("se");
        this.appendDummyInput()
            .appendField(", faça");
        this.appendStatementInput("DO")
            .setCheck(null);
        this.setInputsInline(true);
        this.setPreviousStatement(true, null);
        this.setNextStatement(true, null);
        this.setColour(290);
        this.setTooltip("Executa comandos se a condição for verdadeira");
    }
};

Blockly.Blocks['nqc_se_faca_senao'] = {
    init: function () {
        this.appendValueInput("CONDICAO")
            .setCheck("Boolean")
            .appendField("se");
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
        this.setColour(290);
        this.setTooltip("Executa comandos se a condição for verdadeira, senão executa outros comandos");
    }
};

// ============ SEÇÃO 8: TAREFAS ============

Blockly.Blocks['nqc_tarefa_principal'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("tarefa principal");
        this.appendStatementInput("STATEMENTS")
            .setCheck(null);
        this.setColour(180);
        this.setTooltip("Tarefa principal do programa (task main)");
    },
    
    /**
     * Verifica se já existe outro bloco de tarefa principal no workspace
     * @return {boolean} true se já existe, false caso contrário
     */
    onchange: function(event) {
        if (!this.workspace) {
            return;
        }
        
        // Contar blocos de tarefa principal no workspace
        var mainTaskBlocks = this.workspace.getBlocksByType('nqc_tarefa_principal', false);
        
        // Se há mais de um bloco de tarefa principal, desabilitar este bloco se não for o primeiro
        if (mainTaskBlocks.length > 1) {
            var firstBlock = mainTaskBlocks[0];
            if (this.id !== firstBlock.id) {
                this.setWarningText('Apenas um bloco de "tarefa principal" é permitido no projeto. Remova este bloco.');
                this.setEnabled(false);
            }
        } else {
            this.setWarningText(null);
            this.setEnabled(true);
        }
    }
};

Blockly.Blocks['nqc_tarefa_nomeada'] = {
    init: function () {
        this.appendDummyInput()
            .appendField("tarefa")
            .appendField(new Blockly.FieldTextInput("minhaTarefa"), "NOME");
        this.appendStatementInput("STATEMENTS")
            .setCheck(null);
        this.setColour(180);
        this.setTooltip("Tarefa com nome personalizado");
    }
};

// ============ GERADORES DE CÓDIGO NQC ============

// SEÇÃO 1: Motores e Sons
nqc.nqcGenerator.forBlock['nqc_ligar_motor_com_potencia'] = function (block, generator) {
    const motor = block.getFieldValue('MOTOR');
    const sentido = block.getFieldValue('SENTIDO');
    const potenciaPercent = generator.valueToCode(block, 'POTENCIA', generator.ORDER_NONE) || '50';

    // Converter percentual (10-100) para potência NQC (0-7)
    const potenciaNQC = `(${potenciaPercent} * 7 / 100)`;

    let code = `SetPower(${motor}, ${potenciaNQC});\n`;
    code += sentido === 'FWD' ? `OnFwd(${motor});\n` : `OnRev(${motor});\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_ligar_motor'] = function (block, generator) {
    const motor = block.getFieldValue('MOTOR');
    const sentido = block.getFieldValue('SENTIDO');
    return sentido === 'FWD' ? `OnFwd(${motor});\n` : `OnRev(${motor});\n`;
};

nqc.nqcGenerator.forBlock['nqc_define_potencia_percent'] = function (block, generator) {
    const motor = block.getFieldValue('MOTOR');
    const potenciaPercent = generator.valueToCode(block, 'POTENCIA', generator.ORDER_NONE) || '50';
    const potenciaNQC = `(${potenciaPercent} * 7 / 100)`;
    return `SetPower(${motor}, ${potenciaNQC});\n`;
};

nqc.nqcGenerator.forBlock['nqc_define_sentido'] = function (block, generator) {
    const motor = block.getFieldValue('MOTOR');
    const sentido = block.getFieldValue('SENTIDO');
    return sentido === 'FWD' ? `SetDirection(${motor}, OUT_FWD);\n` : `SetDirection(${motor}, OUT_REV);\n`;
};

nqc.nqcGenerator.forBlock['nqc_toca_som'] = function (block, generator) {
    const som = block.getFieldValue('SOM');
    return `PlaySound(${som});\n`;
};

// SEÇÃO 2: Sensores
nqc.nqcGenerator.forBlock['nqc_define_sensor_toque'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `ClearSensor(${sensor});\n`;
    code += `SetSensorType(${sensor}, SENSOR_TOUCH);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_BOOL);\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_define_sensor_luz'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `ClearSensor(${sensor});\n`;
    code += `SetSensorType(${sensor}, SENSOR_LIGHT);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_PERCENT);\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_define_sensor_rotacao'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `ClearSensor(${sensor});\n`;
    code += `SetSensorType(${sensor}, SENSOR_ROTATION);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_RAW);\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_valor_sensor_toque'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    return [sensor, generator.ORDER_ATOMIC];
};

nqc.nqcGenerator.forBlock['nqc_valor_sensor_luz'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    return [sensor, generator.ORDER_ATOMIC];
};

nqc.nqcGenerator.forBlock['nqc_valor_sensor_rotacao'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    return [sensor, generator.ORDER_ATOMIC];
};

// SEÇÃO 3: Temporização e Loops
nqc.nqcGenerator.forBlock['nqc_espera_segundos'] = function (block, generator) {
    const seconds = generator.valueToCode(block, 'SECONDS', generator.ORDER_NONE) || '1';
    // Multiplicar por 100 para converter segundos em ticks
    return `Wait(${seconds} * 100);\n`;
};

nqc.nqcGenerator.forBlock['nqc_espera_ate_que'] = function (block, generator) {
    const condicao = generator.valueToCode(block, 'CONDICAO', generator.ORDER_NONE) || 'true';
    return `until(${condicao});\n`;
};

nqc.nqcGenerator.forBlock['nqc_repita_vezes'] = function (block, generator) {
    const times = generator.valueToCode(block, 'TIMES', generator.ORDER_NONE) || '10';
    const branch = generator.statementToCode(block, 'DO');
    return `repeat(${times}) {\n${branch}}\n`;
};

nqc.nqcGenerator.forBlock['nqc_repita_infinitamente'] = function (block, generator) {
    const branch = generator.statementToCode(block, 'DO');
    return `while(true) {\n${branch}}\n`;
};

nqc.nqcGenerator.forBlock['nqc_repita_ate_que'] = function (block, generator) {
    const condicao = generator.valueToCode(block, 'CONDICAO', generator.ORDER_NONE) || 'false';
    const branch = generator.statementToCode(block, 'DO');
    return `do {\n${branch}} while (!(${condicao}));\n`;
};

// SEÇÃO 4: Variáveis
nqc.nqcGenerator.forBlock['nqc_variavel_recebe'] = function (block, generator) {
    const varName = block.getFieldValue('VAR');
    const valor = generator.valueToCode(block, 'VALOR', generator.ORDER_ASSIGNMENT) || '0';
    generator.registerVariableInScope(varName);
    return `${varName} = ${valor};\n`;
};

nqc.nqcGenerator.forBlock['nqc_valor_variavel'] = function (block, generator) {
    const varName = block.getFieldValue('VAR');
    generator.registerVariableInScope(varName);
    return [varName, generator.ORDER_ATOMIC];
};

// SEÇÃO 5: Matemática
nqc.nqcGenerator.forBlock['nqc_numero'] = function (block, generator) {
    const num = block.getFieldValue('NUM');
    return [num, generator.ORDER_ATOMIC];
};

nqc.nqcGenerator.forBlock['nqc_operacao_matematica'] = function (block, generator) {
    const OPERATORS = {
        'ADD': [' + ', generator.ORDER_ADDITIVE],
        'MINUS': [' - ', generator.ORDER_ADDITIVE],
        'MULTIPLY': [' * ', generator.ORDER_MULTIPLICATIVE],
        'DIVIDE': [' / ', generator.ORDER_MULTIPLICATIVE]
    };
    const tuple = OPERATORS[block.getFieldValue('OP')];
    const operator = tuple[0];
    const order = tuple[1];
    const argument0 = generator.valueToCode(block, 'A', order) || '0';
    const argument1 = generator.valueToCode(block, 'B', order) || '0';
    const code = argument0 + operator + argument1;
    return [code, order];
};

// SEÇÃO 6: Lógica
nqc.nqcGenerator.forBlock['nqc_comparacao'] = function (block, generator) {
    const OPERATORS = {
        'EQ': '==',
        'NEQ': '!=',
        'LT': '<',
        'LTE': '<=',
        'GT': '>',
        'GTE': '>='
    };
    const operator = OPERATORS[block.getFieldValue('OP')];
    const order = generator.ORDER_RELATIONAL;
    const argument0 = generator.valueToCode(block, 'A', order) || '0';
    const argument1 = generator.valueToCode(block, 'B', order) || '0';
    const code = argument0 + ' ' + operator + ' ' + argument1;
    return [code, order];
};

nqc.nqcGenerator.forBlock['nqc_operacao_logica'] = function (block, generator) {
    const operator = (block.getFieldValue('OP') === 'AND') ? '&&' : '||';
    const order = (operator === '&&') ? generator.ORDER_LOGICAL_AND : generator.ORDER_LOGICAL_OR;
    const argument0 = generator.valueToCode(block, 'A', order) || 'false';
    const argument1 = generator.valueToCode(block, 'B', order) || 'false';
    const code = argument0 + ' ' + operator + ' ' + argument1;
    return [code, order];
};

nqc.nqcGenerator.forBlock['nqc_contrario'] = function (block, generator) {
    const argument0 = generator.valueToCode(block, 'BOOL', generator.ORDER_UNARY_PREFIX) || 'true';
    const code = '!' + argument0;
    return [code, generator.ORDER_UNARY_PREFIX];
};

nqc.nqcGenerator.forBlock['nqc_booleano'] = function (block, generator) {
    const code = (block.getFieldValue('BOOL') === 'TRUE') ? 'true' : 'false';
    return [code, generator.ORDER_ATOMIC];
};

// SEÇÃO 7: Condicionais
nqc.nqcGenerator.forBlock['nqc_se_faca'] = function (block, generator) {
    const condicao = generator.valueToCode(block, 'CONDICAO', generator.ORDER_NONE) || 'false';
    const branch = generator.statementToCode(block, 'DO');
    return `if (${condicao}) {\n${branch}}\n`;
};

nqc.nqcGenerator.forBlock['nqc_se_faca_senao'] = function (block, generator) {
    const condicao = generator.valueToCode(block, 'CONDICAO', generator.ORDER_NONE) || 'false';
    const branchDo = generator.statementToCode(block, 'DO');
    const branchElse = generator.statementToCode(block, 'ELSE');
    return `if (${condicao}) {\n${branchDo}} else {\n${branchElse}}\n`;
};

// SEÇÃO 8: Tarefas
nqc.nqcGenerator.forBlock['nqc_tarefa_principal'] = function (block, generator) {
    const scopeId = 'task_main';
    
    // Marcar que task main foi encontrada
    generator.hasMainTask_ = true;
    
    // Iniciar rastreamento de escopo
    generator.startScope(scopeId);
    
    // Coletar todas as variáveis usadas no bloco
    const statements_block = block.getInputTargetBlock('STATEMENTS');
    if (statements_block) {
        generator.collectVariablesInBlock(statements_block);
    }
    
    // Gerar código das instruções
    const statements = generator.statementToCode(block, 'STATEMENTS');
    
    // Obter declarações de variáveis
    const varDeclarations = generator.getVariableDeclarations(scopeId);
    
    // Finalizar escopo
    generator.endScope();
    
    // Montar código da tarefa com declarações no topo
    return `task main()\n{\n${varDeclarations}${statements}}\n`;
};

nqc.nqcGenerator.forBlock['nqc_tarefa_nomeada'] = function (block, generator) {
    const nome = block.getFieldValue('NOME') || 'minhaTarefa';
    const scopeId = 'task_' + nome;
    
    // Iniciar rastreamento de escopo
    generator.startScope(scopeId);
    
    // Coletar todas as variáveis usadas no bloco
    const statements_block = block.getInputTargetBlock('STATEMENTS');
    if (statements_block) {
        generator.collectVariablesInBlock(statements_block);
    }
    
    // Gerar código das instruções
    const statements = generator.statementToCode(block, 'STATEMENTS');
    
    // Obter declarações de variáveis
    const varDeclarations = generator.getVariableDeclarations(scopeId);
    
    // Finalizar escopo
    generator.endScope();
    
    // Montar código da tarefa com declarações no topo
    return `task ${nome}()\n{\n${varDeclarations}${statements}}\n`;
};

console.log('[NQC-BLOCKS] Blocos e geradores NQC carregados');
