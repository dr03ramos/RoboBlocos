// ============================================================================
// Geradores de Código NQC para Blocos Customizados
// ============================================================================
// Este arquivo contém os geradores de código para cada bloco.
// As definições de interface dos blocos estão em nqc-blocks-definitions.js
// Este arquivo deve ser carregado depois de nqc-generator.js e nqc-blocks-definitions.js
// ============================================================================

// ============ SEÇÃO 1: MOTORES E SONS ============

nqc.nqcGenerator.forBlock['nqc_ligar_motor_com_potencia'] = function (block, generator) {
    const motor = block.getFieldValue('MOTOR');
    const sentido = block.getFieldValue('SENTIDO');
    const potenciaPercent = generator.valueToCode(block, 'POTENCIA', generator.ORDER_NONE) || '50';

    // Converter percentual (0-100) para potência NQC (0-7)
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

nqc.nqcGenerator.forBlock['nqc_desligar_motor'] = function (block, generator) {
    const motor = block.getFieldValue('MOTOR');
    return `Off(${motor});\n`;
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

// ============ SEÇÃO 2: SENSORES ============

nqc.nqcGenerator.forBlock['nqc_define_sensor_toque'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `SetSensorType(${sensor}, SENSOR_TYPE_TOUCH);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_BOOL);\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_define_sensor_luz'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `SetSensorType(${sensor}, SENSOR_TYPE_LIGHT);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_PERCENT);\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_define_sensor_rotacao'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `SetSensorType(${sensor}, SENSOR_TYPE_ROTATION);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_ROTATION);\n`;
    code += `ClearSensor(${sensor});\n`;
    return code;
};

nqc.nqcGenerator.forBlock['nqc_define_sensor_temperatura'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    let code = `SetSensorType(${sensor}, SENSOR_TYPE_TEMPERATURE);\n`;
    code += `SetSensorMode(${sensor}, SENSOR_MODE_CELSIUS);\n`;
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

nqc.nqcGenerator.forBlock['nqc_valor_sensor_temperatura'] = function (block, generator) {
    const sensor = block.getFieldValue('SENSOR');
    return [sensor, generator.ORDER_ATOMIC];
};

// ============ SEÇÃO 3: TEMPORIZAÇÃO E LOOPS ============

nqc.nqcGenerator.forBlock['nqc_espera_segundos'] = function (block, generator) {
    const seconds = generator.valueToCode(block, 'SECONDS', generator.ORDER_NONE) || '1';
    // Multiplicar por 100 para converter segundos em ticks (unidade de tempo do NQC)
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

// ============ SEÇÃO 4: VARIÁVEIS ============

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

// ============ SEÇÃO 5: MATEMÁTICA ============

nqc.nqcGenerator.forBlock['nqc_numero'] = function (block, generator) {
    const num = block.getFieldValue('NUM');
    return [num, generator.ORDER_ATOMIC];
};

nqc.nqcGenerator.forBlock['nqc_percentual'] = function (block, generator) {
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
    const code = argument0 + operator + ' ' + argument1;
    return [code, order];
};

// ============ SEÇÃO 6: LÓGICA ============

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

// ============ SEÇÃO 7: CONDICIONAIS ============

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

// ============ SEÇÃO 8: TAREFAS ============

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

nqc.nqcGenerator.forBlock['nqc_executar_tarefa'] = function (block, generator) {
    const nome = block.getFieldValue('NOME') || 'minhaTarefa';
    return `start ${nome};\n`;
};

nqc.nqcGenerator.forBlock['nqc_interromper_tarefa'] = function (block, generator) {
    const nome = block.getFieldValue('NOME') || 'minhaTarefa';
    return `stop ${nome};\n`;
};

console.log('[NQC-BLOCKS-GENERATORS] Geradores de código NQC carregados');
