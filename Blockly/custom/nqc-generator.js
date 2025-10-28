/* eslint-disable */
; (function (root, factory) {
    if (typeof define === 'function' && define.amd) { // AMD
        define(["./lib/blockly_compressed.js"], factory);
    } else if (typeof exports === 'object') { // Node.js
        module.exports = factory(require("./lib/blockly_compressed.js"));
    } else { // Script
        root.nqc = factory(root.Blockly);
        root.Blockly.Nqc = root.nqc.nqcGenerator;
    }
}(this, function (__parent__) {

    'use strict';

    const Blockly = __parent__;

    /**
     * NQC code generator class.
     */
    class NqcGenerator extends Blockly.Generator {
        constructor() {
            super('NQC');

            // List of illegal variable names.
            this.addReservedWords(
                'task,sub,inline,void,int,const,asm,abs,sign,if,else,while,do,for,repeat,' +
                'switch,case,default,break,continue,until,return,monitor,catch,acquire,release,' +
                'Random,OnFwd,OnRev,Off,Float,Fwd,Rev,Toggle,SetPower,SetDirection,ClearTimer,' +
                'Timer,Wait,PlaySound,PlayTone,ClearMessage,SendMessage,Message,SetSensor,' +
                'SetSensorType,SetSensorMode,ClearSensor,Sensor,SensorValue,SensorType,SensorMode,' +
                'StartTask,StopTask,StopAllTasks,SetPriority,OUT_A,OUT_B,OUT_C,SENSOR_1,SENSOR_2,' +
                'SENSOR_3,true,false'
            );

            this.ORDER_ATOMIC = 0;           // 0 "" ...
            this.ORDER_UNARY_POSTFIX = 1;    // expr++ expr-- () []
            this.ORDER_UNARY_PREFIX = 2;     // ++expr --expr +expr -expr ~ !
            this.ORDER_MULTIPLICATIVE = 3;   // * / %
            this.ORDER_ADDITIVE = 4;         // + -
            this.ORDER_SHIFT = 5;            // << >>
            this.ORDER_RELATIONAL = 6;       // < <= > >=
            this.ORDER_EQUALITY = 7;         // == !=
            this.ORDER_BITWISE_AND = 8;      // &
            this.ORDER_BITWISE_XOR = 9;      // ^
            this.ORDER_BITWISE_OR = 10;      // |
            this.ORDER_LOGICAL_AND = 11;     // &&
            this.ORDER_LOGICAL_OR = 12;      // ||
            this.ORDER_CONDITIONAL = 13;     // ?:
            this.ORDER_ASSIGNMENT = 14;      // = += -= *= /= %= <<= >>= &= ^= |=
            this.ORDER_NONE = 99;            // (...)

            // Sistema customizado de rastreamento de variáveis por escopo
            this.scopeVariables_ = {};
            this.currentScope_ = null;
            this.hasMainTask_ = false;
        }

        /**
         * Inicializa o gerador para um novo workspace
         */
        init(workspace) {
            super.init(workspace);

            if (!this.nameDB_) {
                this.nameDB_ = new Blockly.Names(this.RESERVED_WORDS_);
            } else {
                this.nameDB_.reset();
            }

            this.nameDB_.setVariableMap(workspace.getVariableMap());
            this.nameDB_.populateVariables(workspace);
            this.nameDB_.populateProcedures(workspace);

            // NÃO usar o sistema padrão de variáveis globais
            // this.definitions_['variables'] = ... (removido)
            
            // Resetar o sistema de rastreamento de variáveis por escopo
            this.scopeVariables_ = {};
            this.currentScope_ = null;
            this.hasMainTask_ = false;
        }

        /**
         * Inicia um novo escopo (task, função, etc.)
         * @param {string} scopeId - Identificador único do escopo
         */
        startScope(scopeId) {
            this.currentScope_ = scopeId;
            if (!this.scopeVariables_[scopeId]) {
                this.scopeVariables_[scopeId] = new Set();
            }
        }

        /**
         * Finaliza o escopo atual
         */
        endScope() {
            this.currentScope_ = null;
        }

        /**
         * Registra o uso de uma variável no escopo atual
         * @param {string} varName - Nome da variável
         */
        registerVariableInScope(varName) {
            if (this.currentScope_ && varName) {
                this.scopeVariables_[this.currentScope_].add(varName);
            }
        }

        /**
         * Obtém as declarações de variáveis para um escopo
         * @param {string} scopeId - Identificador do escopo
         * @return {string} - Declarações de variáveis
         */
        getVariableDeclarations(scopeId) {
            const variables = this.scopeVariables_[scopeId];
            if (!variables || variables.size === 0) {
                return '';
            }
            
            const declarations = Array.from(variables).map(varName => `  int ${varName};`).join('\n');
            return declarations + '\n\n';
        }

        /**
         * Coleta todas as variáveis usadas em um bloco e seus filhos
         * @param {Blockly.Block} block - Bloco raiz
         * @return {Set<string>} - Conjunto de nomes de variáveis
         */
        collectVariablesInBlock(block) {
            const variables = new Set();
            
            if (!block) {
                return variables;
            }

            // Verificar se o bloco usa variáveis
            if (block.type === 'nqc_variavel_recebe' || block.type === 'variables_set') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            } else if (block.type === 'nqc_valor_variavel' || block.type === 'variables_get') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            } else if (block.type === 'math_change') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            } else if (block.type === 'controls_for' || block.type === 'controls_forEach') {
                const varName = block.getFieldValue('VAR');
                if (varName) {
                    variables.add(varName);
                }
            }

            // Recursivamente coletar variáveis de blocos filhos (inputs)
            for (let i = 0; i < block.inputList.length; i++) {
                const input = block.inputList[i];
                if (input.connection && input.connection.targetBlock()) {
                    const childVars = this.collectVariablesInBlock(input.connection.targetBlock());
                    childVars.forEach(v => variables.add(v));
                }
            }

            // Recursivamente coletar variáveis de blocos seguintes
            if (block.nextConnection && block.nextConnection.targetBlock()) {
                const nextVars = this.collectVariablesInBlock(block.nextConnection.targetBlock());
                nextVars.forEach(v => variables.add(v));
            }

            return variables;
        }

        /**
         * Converte workspace em código, garantindo que tenha task main
         * @param {Blockly.Workspace} workspace - Workspace do Blockly
         * @return {string} - Código NQC completo
         */
        workspaceToCode(workspace) {
            if (!workspace) {
                return '';
            }

            this.init(workspace);
            
            const allBlocks = workspace.getTopBlocks(true);
            let taskCode = '';
            let looseCode = '';
            let looseBlocks = [];
            
            // Primeiro, processar todos os blocos
            for (let i = 0; i < allBlocks.length; i++) {
                const block = allBlocks[i];
                
                // Verificar se é um bloco de tarefa (principal ou nomeada)
                if (block.type === 'nqc_tarefa_principal' || block.type === 'nqc_tarefa_nomeada') {
                    const blockCode = this.blockToCode(block);
                    if (blockCode) {
                        taskCode += blockCode;
                        if (!blockCode.endsWith('\n')) {
                            taskCode += '\n';
                        }
                    }
                } else {
                    // Bloco solto - guardar para processar depois
                    looseBlocks.push(block);
                }
            }

            // Processar blocos soltos
            if (looseBlocks.length > 0) {
                // Se NÃO há task main, criar uma com os blocos soltos dentro
                if (!this.hasMainTask_) {
                    const scopeId = 'task_main';
                    
                    // Iniciar rastreamento de escopo
                    this.startScope(scopeId);
                    
                    // Coletar variáveis de todos os blocos soltos
                    for (let i = 0; i < looseBlocks.length; i++) {
                        this.collectVariablesInBlock(looseBlocks[i]);
                    }
                    
                    // Gerar código dos blocos soltos
                    for (let i = 0; i < looseBlocks.length; i++) {
                        const blockCode = this.blockToCode(looseBlocks[i]);
                        if (blockCode) {
                            looseCode += '  ' + blockCode.trim().replace(/\n/g, '\n  ') + '\n';
                        }
                    }
                    
                    // Obter declarações de variáveis
                    const varDeclarations = this.getVariableDeclarations(scopeId);
                    
                    // Finalizar escopo
                    this.endScope();
                    
                    // Criar task main com declarações e blocos soltos
                    taskCode += '\ntask main()\n{\n';
                    if (varDeclarations) {
                        taskCode += varDeclarations;
                    }
                    taskCode += looseCode;
                    taskCode += '}\n';
                } else {
                    // Se JÁ existe task main, gerar os blocos soltos normalmente (fora de qualquer tarefa)
                    // Isso permite código de inicialização ou definições globais
                    for (let i = 0; i < looseBlocks.length; i++) {
                        const blockCode = this.blockToCode(looseBlocks[i]);
                        if (blockCode) {
                            looseCode += blockCode;
                            if (!blockCode.endsWith('\n')) {
                                looseCode += '\n';
                            }
                        }
                    }
                    
                    // Adicionar blocos soltos ANTES das tarefas
                    taskCode = looseCode + '\n' + taskCode;
                }
            } else if (!this.hasMainTask_) {
                // Não há blocos soltos E não há task main - criar task main vazia
                taskCode += '\ntask main()\n{\n}\n';
            }

            return this.finish(taskCode);
        }

        /**
         * Prepend the generated code with definitions.
         * @param {string} code Generated code.
         * @return {string} Completed code.
         */
        finish(code) {
            const imports = [];
            const definitions = [];
            for (let name in this.definitions_) {
                // Ignorar 'variables' do sistema padrão
                if (name === 'variables') {
                    continue;
                }
                
                const def = this.definitions_[name];
                if (def.match(/^#include/)) {
                    imports.push(def);
                } else {
                    definitions.push(def);
                }
            }

            const allDefs = (imports.length ? imports.join('\n') + '\n\n' : '') +
                (definitions.length ? definitions.join('\n\n') + '\n\n' : '');
            return allDefs.replace(/\n\n+/g, '\n\n').replace(/\n*$/, '\n') + code;
        }

        /**
         * Naked values are top-level blocks with outputs that aren't plugged into
         * anything. A trailing semicolon is needed to make this legal.
         * @param {string} line Line of generated code.
         * @return {string} Legal line of code.
         */
        scrubNakedValue(line) {
            return line + ';\n';
        }

        /**
         * Encode a string as a properly escaped NQC string, complete with quotes.
         * @param {string} string Text to encode.
         * @return {string} NQC string.
         */
        quote_(string) {
            string = string.replace(/\\/g, '\\\\')
                .replace(/\n/g, '\\n')
                .replace(/"/g, '\\"');
            return '"' + string + '"';
        }

        /**
         * Common tasks for generating NQC from blocks.
         * @param {!Blockly.Block} block Current block.
         * @param {string} code The NQC code created for this block.
         * @param {boolean=} opt_thisOnly True to generate code for only this statement.
         * @return {string} NQC code with comments and subsequent blocks added.
         */
        scrub_(block, code, opt_thisOnly) {
            let commentCode = '';
            if (!block.outputConnection || !block.outputConnection.targetConnection) {
                const comment = block.getCommentText();
                if (comment) {
                    comment.split('\n').forEach(function (line) {
                        commentCode += '// ' + line.trim() + '\n';
                    });
                }
                for (let i = 0; i < block.inputList.length; i++) {
                    if (block.inputList[i].type === 1) { // 1 = VALUE input type
                        const childBlock = block.inputList[i].connection && block.inputList[i].connection.targetBlock();
                        if (childBlock) {
                            const childComment = this.allNestedComments(childBlock);
                            if (childComment) {
                                commentCode += childComment;
                            }
                        }
                    }
                }
            }
            const nextBlock = block.nextConnection && block.nextConnection.targetBlock();
            const nextCode = opt_thisOnly ? '' : this.blockToCode(nextBlock);
            return commentCode + code + nextCode;
        }
    }

    // Create generator instance
    const nqcGenerator = new NqcGenerator();

    // ===== LOGIC BLOCKS =====

    nqcGenerator.forBlock['controls_if'] = function (block, generator) {
        let n = 0;
        let code = '', branchCode, conditionCode;
        if (generator.STATEMENT_PREFIX) {
            code += generator.injectId(generator.STATEMENT_PREFIX, block);
        }
        do {
            conditionCode = generator.valueToCode(block, 'IF' + n,
                generator.ORDER_NONE) || 'false';
            branchCode = generator.statementToCode(block, 'DO' + n);
            if (generator.STATEMENT_SUFFIX) {
                branchCode = generator.prefixLines(
                    generator.injectId(generator.STATEMENT_SUFFIX, block),
                    generator.INDENT) + branchCode;
            }
            code += (n > 0 ? ' else ' : '') +
                'if (' + conditionCode + ') {\n' + branchCode + '}';
            n++;
        } while (block.getInput('IF' + n));

        if (block.getInput('ELSE') || generator.STATEMENT_SUFFIX) {
            branchCode = generator.statementToCode(block, 'ELSE');
            if (generator.STATEMENT_SUFFIX) {
                branchCode = generator.prefixLines(
                    generator.injectId(generator.STATEMENT_SUFFIX, block),
                    generator.INDENT) + branchCode;
            }
            code += ' else {\n' + branchCode + '}'
        }
        return code + '\n';
    };

    nqcGenerator.forBlock['logic_compare'] = function (block, generator) {
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

    nqcGenerator.forBlock['logic_operation'] = function (block, generator) {
        const operator = (block.getFieldValue('OP') === 'AND') ? '&&' : '||';
        const order = (operator === '&&') ? generator.ORDER_LOGICAL_AND :
            generator.ORDER_LOGICAL_OR;
        let argument0 = generator.valueToCode(block, 'A', order);
        let argument1 = generator.valueToCode(block, 'B', order);
        if (!argument0 && !argument1) {
            argument0 = 'false';
            argument1 = 'false';
        } else {
            const defaultArgument = (operator === '&&') ? 'true' : 'false';
            if (!argument0) {
                argument0 = defaultArgument;
            }
            if (!argument1) {
                argument1 = defaultArgument;
            }
        }
        const code = argument0 + ' ' + operator + ' ' + argument1;
        return [code, order];
    };

    nqcGenerator.forBlock['logic_negate'] = function (block, generator) {
        const order = generator.ORDER_UNARY_PREFIX;
        const argument0 = generator.valueToCode(block, 'BOOL', order) || 'true';
        const code = '!' + argument0;
        return [code, order];
    };

    nqcGenerator.forBlock['logic_boolean'] = function (block, generator) {
        const code = (block.getFieldValue('BOOL') === 'TRUE') ? 'true' : 'false';
        return [code, generator.ORDER_ATOMIC];
    };

    nqcGenerator.forBlock['logic_null'] = function (block, generator) {
        return ['0', generator.ORDER_ATOMIC];
    };

    nqcGenerator.forBlock['logic_ternary'] = function (block, generator) {
        const value_if = generator.valueToCode(block, 'IF',
            generator.ORDER_CONDITIONAL) || 'false';
        const value_then = generator.valueToCode(block, 'THEN',
            generator.ORDER_CONDITIONAL) || '0';
        const value_else = generator.valueToCode(block, 'ELSE',
            generator.ORDER_CONDITIONAL) || '0';
        const code = value_if + ' ? ' + value_then + ' : ' + value_else;
        return [code, generator.ORDER_CONDITIONAL];
    };

    // ===== LOOP BLOCKS =====

    nqcGenerator.forBlock['controls_repeat_ext'] = function (block, generator) {
        let repeats;
        if (block.getField('TIMES')) {
            repeats = String(Number(block.getFieldValue('TIMES')));

        } else {
            repeats = generator.valueToCode(block, 'TIMES',
                generator.ORDER_NONE) || '0';
        }
        let branch = generator.statementToCode(block, 'DO');
        branch = generator.addLoopTrap(branch, block);
        let code = '';
        let endVar = repeats;
        if (!repeats.match(/^\w+$/) && !repeats.match(/^\d+$/)) {
            const endVarName = generator.nameDB_.getDistinctName(
                'repeat_end', Blockly.Names.NameType.VARIABLE);
            generator.registerVariableInScope(endVarName);
            endVar = endVarName;
            code += endVarName + ' = ' + repeats + ';\n';
        }
        code += 'repeat(' + endVar + ') {\n' +
            branch + '}\n';
        return code;
    };

    nqcGenerator.forBlock['controls_repeat'] =
        nqcGenerator.forBlock['controls_repeat_ext'];

    nqcGenerator.forBlock['controls_whileUntil'] = function (block, generator) {
        const until = block.getFieldValue('MODE') === 'UNTIL';
        let argument0 = generator.valueToCode(block, 'BOOL',
            until ? generator.ORDER_LOGICAL_NOT :
                generator.ORDER_NONE) || 'false';
        let branch = generator.statementToCode(block, 'DO');
        branch = generator.addLoopTrap(branch, block);
        if (until) {
            argument0 = '!' + argument0;
        }
        return 'while (' + argument0 + ') {\n' + branch + '}\n';
    };

    nqcGenerator.forBlock['controls_for'] = function (block, generator) {
        const variable0 = generator.nameDB_.getName(
            block.getFieldValue('VAR'), Blockly.Names.NameType.VARIABLE);
        generator.registerVariableInScope(variable0);
        
        const argument0 = generator.valueToCode(block, 'FROM',
            generator.ORDER_ASSIGNMENT) || '0';
        const argument1 = generator.valueToCode(block, 'TO',
            generator.ORDER_ASSIGNMENT) || '0';
        const increment = generator.valueToCode(block, 'BY',
            generator.ORDER_ASSIGNMENT) || '1';
        let branch = generator.statementToCode(block, 'DO');
        branch = generator.addLoopTrap(branch, block);
        let code = '';
        let up;
        if (argument0.match(/^\d+$/) && argument1.match(/^\d+$/) &&
            increment.match(/^\d+$/)) {
            up = Number(argument0) <= Number(argument1);
        } else {
            code += '';
            up = true;
        }
        code += 'for (' + variable0 + ' = ' + argument0 + '; ' +
            variable0 + (up ? ' <= ' : ' >= ') + argument1 + '; ' +
            variable0;
        if (increment.match(/^\d+$/)) {
            code += (Number(increment) === 1) ? '++' :
                ' += ' + increment;
        } else {
            code += ' += ' + increment;
        }
        code += ') {\n' + branch + '}\n';
        return code;
    };

    nqcGenerator.forBlock['controls_forEach'] = function (block, generator) {
        const variable0 = generator.nameDB_.getName(
            block.getFieldValue('VAR'), Blockly.Names.NameType.VARIABLE);
        generator.registerVariableInScope(variable0);
        
        const argument0 = generator.valueToCode(block, 'LIST',
            generator.ORDER_ASSIGNMENT) || '[]';
        let branch = generator.statementToCode(block, 'DO');
        branch = generator.addLoopTrap(branch, block);
        const code = 'for (' + variable0 + ' in ' + argument0 + ') {\n' + branch + '}\n';
        return code;
    };

    nqcGenerator.forBlock['controls_flow_statements'] = function (block, generator) {
        let xfix = '';
        if (generator.STATEMENT_PREFIX) {
            xfix += generator.injectId(generator.STATEMENT_PREFIX, block);
        }
        if (generator.STATEMENT_SUFFIX) {
            xfix += generator.injectId(generator.STATEMENT_SUFFIX, block);
        }
        if (generator.STATEMENT_PREFIX) {
            const loop = block.getSurroundLoop();
            if (loop && !loop.suppressPrefixSuffix) {
                xfix += generator.injectId(generator.STATEMENT_PREFIX, loop);
            }
        }
        switch (block.getFieldValue('FLOW')) {
            case 'BREAK':
                return xfix + 'break;\n';
            case 'CONTINUE':
                return xfix + 'continue;\n';
        }
        throw Error('Unknown flow statement.');
    };

    // ===== MATH BLOCKS =====

    nqcGenerator.forBlock['math_number'] = function (block, generator) {
        const code = Number(block.getFieldValue('NUM'));
        const order = code >= 0 ? generator.ORDER_ATOMIC : generator.ORDER_UNARY_PREFIX;
        return [code, order];
    };

    nqcGenerator.forBlock['math_arithmetic'] = function (block, generator) {
        const OPERATORS = {
            'ADD': [' + ', generator.ORDER_ADDITIVE],
            'MINUS': [' - ', generator.ORDER_ADDITIVE],
            'MULTIPLY': [' * ', generator.ORDER_MULTIPLICATIVE],
            'DIVIDE': [' / ', generator.ORDER_MULTIPLICATIVE],
            'POWER': [null, generator.ORDER_NONE]
        };
        const tuple = OPERATORS[block.getFieldValue('OP')];
        const operator = tuple[0];
        const order = tuple[1];
        const argument0 = generator.valueToCode(block, 'A', order) || '0';
        const argument1 = generator.valueToCode(block, 'B', order) || '0';
        const code = argument0 + operator + argument1;
        return [code, order];
    };

    nqcGenerator.forBlock['math_single'] = function (block, generator) {
        const operator = block.getFieldValue('OP');
        let code;
        let arg;
        if (operator === 'NEG') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_UNARY_PREFIX) || '0';
            return ['-' + arg, generator.ORDER_UNARY_PREFIX];
        }
        if (operator === 'ABS') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_NONE) || '0';
            code = 'abs(' + arg + ')';
        } else if (operator === 'ROOT') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_NONE) || '0';
            code = 'sqrt(' + arg + ')';
        } else if (operator === 'LN') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_NONE) || '0';
            code = 'log(' + arg + ')';
        } else if (operator === 'LOG10') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_NONE) || '0';
            code = 'log10(' + arg + ')';
        } else if (operator === 'EXP') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_NONE) || '0';
            code = 'exp(' + arg + ')';
        } else if (operator === 'POW10') {
            arg = generator.valueToCode(block, 'NUM',
                generator.ORDER_NONE) || '0';
            code = 'pow(10, ' + arg + ')';
        }
        if (code) {
            return [code, generator.ORDER_UNARY_POSTFIX];
        }
        switch (operator) {
            case 'ROUND':
                arg = generator.valueToCode(block, 'NUM',
                    generator.ORDER_NONE) || '0';
                code = arg;
                break;
            case 'ROUNDUP':
                arg = generator.valueToCode(block, 'NUM',
                    generator.ORDER_NONE) || '0';
                code = arg;
                break;
            case 'ROUNDDOWN':
                arg = generator.valueToCode(block, 'NUM',
                    generator.ORDER_NONE) || '0';
                code = arg;
                break;
        }
        return [code, generator.ORDER_UNARY_POSTFIX];
    };

    nqcGenerator.forBlock['math_constant'] = function (block, generator) {
        const CONSTANTS = {
            'PI': ['3.14159', generator.ORDER_ATOMIC],
            'E': ['2.71828', generator.ORDER_ATOMIC],
            'GOLDEN_RATIO': ['1.61803', generator.ORDER_ATOMIC],
            'SQRT2': ['1.41421', generator.ORDER_ATOMIC],
            'SQRT1_2': ['0.70711', generator.ORDER_ATOMIC],
            'INFINITY': ['32767', generator.ORDER_ATOMIC]
        };
        return CONSTANTS[block.getFieldValue('CONSTANT')];
    };

    nqcGenerator.forBlock['math_number_property'] = function (block, generator) {
        const number_to_check = generator.valueToCode(block, 'NUMBER_TO_CHECK',
            generator.ORDER_MODULUS) || '0';
        const dropdown_property = block.getFieldValue('PROPERTY');
        let code;
        if (dropdown_property === 'PRIME') {
            code = number_to_check + ' > 1';
        } else if (dropdown_property === 'EVEN') {
            code = number_to_check + ' % 2 == 0';
        } else if (dropdown_property === 'ODD') {
            code = number_to_check + ' % 2 == 1';
        } else if (dropdown_property === 'WHOLE') {
            code = 'true';
        } else if (dropdown_property === 'POSITIVE') {
            code = number_to_check + ' > 0';
        } else if (dropdown_property === 'NEGATIVE') {
            code = number_to_check + ' < 0';
        } else if (dropdown_property === 'DIVISIBLE_BY') {
            const divisor = generator.valueToCode(block, 'DIVISOR',
                generator.ORDER_MODULUS) || '0';
            code = number_to_check + ' % ' + divisor + ' == 0';
        }
        return [code, generator.ORDER_RELATIONAL];
    };

    nqcGenerator.forBlock['math_change'] = function (block, generator) {
        const argument0 = generator.valueToCode(block, 'DELTA',
            generator.ORDER_ADDITIVE) || '0';
        const varName = generator.nameDB_.getName(
            block.getFieldValue('VAR'), Blockly.Names.NameType.VARIABLE);
        generator.registerVariableInScope(varName);
        return varName + ' += ' + argument0 + ';\n';
    };

    nqcGenerator.forBlock['math_round'] = function (block, generator) {
        const argument0 = generator.valueToCode(block, 'NUM',
            generator.ORDER_NONE) || '0';
        return [argument0, generator.ORDER_ATOMIC];
    };

    nqcGenerator.forBlock['math_modulo'] = function (block, generator) {
        const argument0 = generator.valueToCode(block, 'DIVIDEND',
            generator.ORDER_MULTIPLICATIVE) || '0';
        const argument1 = generator.valueToCode(block, 'DIVISOR',
            generator.ORDER_MULTIPLICATIVE) || '0';
        const code = argument0 + ' % ' + argument1;
        return [code, generator.ORDER_MULTIPLICATIVE];
    };

    nqcGenerator.forBlock['math_constrain'] = function (block, generator) {
        const argument0 = generator.valueToCode(block, 'VALUE',
            generator.ORDER_NONE) || '0';
        const argument1 = generator.valueToCode(block, 'LOW',
            generator.ORDER_NONE) || '0';
        const argument2 = generator.valueToCode(block, 'HIGH',
            generator.ORDER_NONE) || '0';
        const code = '(' + argument0 + ' < ' + argument1 + ' ? ' + argument1 +
            ' : (' + argument0 + ' > ' + argument2 + ' ? ' + argument2 + ' : ' +
            argument0 + '))';
        return [code, generator.ORDER_CONDITIONAL];
    };

    nqcGenerator.forBlock['math_random_int'] = function (block, generator) {
        const argument0 = generator.valueToCode(block, 'FROM',
            generator.ORDER_NONE) || '0';
        const argument1 = generator.valueToCode(block, 'TO',
            generator.ORDER_NONE) || '0';
        const code = 'Random(' + argument1 + ' - ' + argument0 + ' + 1) + ' + argument0;
        return [code, generator.ORDER_UNARY_POSTFIX];
    };

    nqcGenerator.forBlock['math_random_float'] = function (block, generator) {
        return ['Random(100) / 100.0', generator.ORDER_UNARY_POSTFIX];
    };

    // ===== VARIABLE BLOCKS =====

    nqcGenerator.forBlock['variables_get'] = function (block, generator) {
        const varName = generator.nameDB_.getName(block.getFieldValue('VAR'),
            Blockly.Names.NameType.VARIABLE);
        generator.registerVariableInScope(varName);
        return [varName, generator.ORDER_ATOMIC];
    };

    nqcGenerator.forBlock['variables_set'] = function (block, generator) {
        const argument0 = generator.valueToCode(block, 'VALUE',
            generator.ORDER_ASSIGNMENT) || '0';
        const varName = generator.nameDB_.getName(
            block.getFieldValue('VAR'), Blockly.Names.NameType.VARIABLE);
        generator.registerVariableInScope(varName);
        return varName + ' = ' + argument0 + ';\n';
    };

    // ===== NQC-SPECIFIC ROBOT CONTROL BLOCKS =====

    nqcGenerator.forBlock['nqc_motor_on_fwd'] = function (block, generator) {
        const output = block.getFieldValue('OUTPUT') || 'OUT_A';
        return 'OnFwd(' + output + ');\n';
    };

    nqcGenerator.forBlock['nqc_motor_on_rev'] = function (block, generator) {
        const output = block.getFieldValue('OUTPUT') || 'OUT_A';
        return 'OnRev(' + output + ');\n';
    };

    nqcGenerator.forBlock['nqc_motor_off'] = function (block, generator) {
        const output = block.getFieldValue('OUTPUT') || 'OUT_A';
        return 'Off(' + output + ');\n';
    };

    nqcGenerator.forBlock['nqc_motor_set_power'] = function (block, generator) {
        const output = block.getFieldValue('OUTPUT') || 'OUT_A';
        const power = generator.valueToCode(block, 'POWER',
            generator.ORDER_NONE) || '7';
        return 'SetPower(' + output + ', ' + power + ');\n';
    };

    nqcGenerator.forBlock['nqc_motor_float'] = function (block, generator) {
        const output = block.getFieldValue('OUTPUT') || 'OUT_A';
        return 'Float(' + output + ');\n';
    };

    nqcGenerator.forBlock['nqc_wait'] = function (block, generator) {
        const time = generator.valueToCode(block, 'TIME',
            generator.ORDER_NONE) || '100';
        return 'Wait(' + time + ');\n';
    };

    nqcGenerator.forBlock['nqc_task_main'] = function (block, generator) {
        const statements = generator.statementToCode(block, 'STATEMENTS');
        const code = 'task main()\n{\n' + statements + '}\n';
        return code;
    };

    nqcGenerator.forBlock['nqc_task_start'] = function (block, generator) {
        const taskName = block.getFieldValue('TASK_NAME') || 'task1';
        return 'StartTask(' + taskName + ');\n';
    };

    nqcGenerator.forBlock['nqc_task_stop'] = function (block, generator) {
        const taskName = block.getFieldValue('TASK_NAME') || 'task1';
        return 'StopTask(' + taskName + ');\n';
    };

    nqcGenerator.forBlock['nqc_sensor_value'] = function (block, generator) {
        const sensor = block.getFieldValue('SENSOR') || 'SENSOR_1';
        return [sensor, generator.ORDER_ATOMIC];
    };

    nqcGenerator.forBlock['nqc_sensor_set_type'] = function (block, generator) {
        const sensor = block.getFieldValue('SENSOR') || 'SENSOR_1';
        const type = block.getFieldValue('TYPE') || 'SENSOR_TOUCH';
        return 'SetSensorType(' + sensor + ', ' + type + ');\n';
    };

    nqcGenerator.forBlock['nqc_sensor_set_mode'] = function (block, generator) {
        const sensor = block.getFieldValue('SENSOR') || 'SENSOR_1';
        const mode = block.getFieldValue('MODE') || 'SENSOR_MODE_RAW';
        return 'SetSensorMode(' + sensor + ', ' + mode + ');\n';
    };

    nqcGenerator.forBlock['nqc_play_sound'] = function (block, generator) {
        const sound = block.getFieldValue('SOUND') || 'SOUND_CLICK';
        return 'PlaySound(' + sound + ');\n';
    };

    nqcGenerator.forBlock['nqc_play_tone'] = function (block, generator) {
        const frequency = generator.valueToCode(block, 'FREQUENCY',
            generator.ORDER_NONE) || '440';
        const duration = generator.valueToCode(block, 'DURATION',
            generator.ORDER_NONE) || '100';
        return 'PlayTone(' + frequency + ', ' + duration + ');\n';
    };

    // Export the generator
    return {
        NqcGenerator: NqcGenerator,
        nqcGenerator: nqcGenerator,
        Order: {
            ATOMIC: nqcGenerator.ORDER_ATOMIC,
            UNARY_POSTFIX: nqcGenerator.ORDER_UNARY_POSTFIX,
            UNARY_PREFIX: nqcGenerator.ORDER_UNARY_PREFIX,
            MULTIPLICATIVE: nqcGenerator.ORDER_MULTIPLICATIVE,
            ADDITIVE: nqcGenerator.ORDER_ADDITIVE,
            SHIFT: nqcGenerator.ORDER_SHIFT,
            RELATIONAL: nqcGenerator.ORDER_RELATIONAL,
            EQUALITY: nqcGenerator.ORDER_EQUALITY,
            BITWISE_AND: nqcGenerator.ORDER_BITWISE_AND,
            BITWISE_XOR: nqcGenerator.ORDER_BITWISE_XOR,
            BITWISE_OR: nqcGenerator.ORDER_BITWISE_OR,
            LOGICAL_AND: nqcGenerator.ORDER_LOGICAL_AND,
            LOGICAL_OR: nqcGenerator.ORDER_LOGICAL_OR,
            CONDITIONAL: nqcGenerator.ORDER_CONDITIONAL,
            ASSIGNMENT: nqcGenerator.ORDER_ASSIGNMENT,
            NONE: nqcGenerator.ORDER_NONE
        }
    };

}));
